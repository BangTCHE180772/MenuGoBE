using AutoMapper;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderDetailServiceTest;

public class ConfirmAsyncTests
{
    private readonly Mock<IOrderDetailRepository> _detailRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<ITableService> _tableServiceMock;
    private readonly Mock<IProductService> _productServiceMock;
    private readonly Mock<IInvoiceDocumentService> _invoiceServiceMock;
    private readonly IMapper _mapper;
    private readonly OrderDetailService _service;

    public ConfirmAsyncTests()
    {
        _detailRepoMock = new Mock<IOrderDetailRepository>();
        _orderRepoMock = new Mock<IOrderRepository>();
        _tableServiceMock = new Mock<ITableService>();
        _productServiceMock = new Mock<IProductService>();
        _invoiceServiceMock = new Mock<IInvoiceDocumentService>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new OrderDetailService(
            _detailRepoMock.Object,
            _orderRepoMock.Object,
            _tableServiceMock.Object,
            _productServiceMock.Object,
            _invoiceServiceMock.Object,
            _mapper);
    }

    [Fact]
    public async Task ReturnsTrue_WhenStatusUpdated()
    {
        // Arrange
        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new OrderDetail { Id = 1, OrderId = 1, ProductId = 1, Quantity = 1, Status = "Waiting" });
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(1, "Confirmed")).ReturnsAsync(true);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.ConfirmAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenUpdateFails()
    {
        // Arrange
        _detailRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync(new OrderDetail { Id = 999, OrderId = 1, ProductId = 1, Quantity = 1, Status = "Waiting" });
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(999, "Confirmed")).ReturnsAsync(false);

        // Act
        var result = await _service.ConfirmAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenOrderDetailNotFound()
    {
        // Arrange
        _detailRepoMock.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((OrderDetail?)null);

        // Act
        var result = await _service.ConfirmAsync(404);

        // Assert
        Assert.False(result);
        _detailRepoMock.Verify(r => r.UpdateStatusAsync(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CallsSaveChanges_OnlyWhenSuccess()
    {
        // Arrange
        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new OrderDetail { Id = 1, OrderId = 1, ProductId = 1, Quantity = 1, Status = "Waiting" });
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(1, "Confirmed")).ReturnsAsync(true);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.ConfirmAsync(1);

        // Assert
        _detailRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DoesNotCallSaveChanges_WhenFails()
    {
        // Arrange
        _detailRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync(new OrderDetail { Id = 999, OrderId = 1, ProductId = 1, Quantity = 1, Status = "Waiting" });
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(999, "Confirmed")).ReturnsAsync(false);

        // Act
        await _service.ConfirmAsync(999);

        // Assert
        _detailRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeductsInventory_WhenNewlyConfirmed()
    {
        // Arrange
        var order = new Order { Id = 1, TableId = 5, Status = "Active" };
        var product = new MenuGoBE.Dtos.Product.ProductViewDto { Id = 1, Name = "Coca Cola", Type = MenuGoBE.Models.Enums.ProductType.Regular };

        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new OrderDetail { Id = 1, OrderId = 1, ProductId = 1, Quantity = 2, Status = "Waiting" });
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(1, "Confirmed")).ReturnsAsync(true);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _tableServiceMock.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(new MenuGoBE.Dtos.Table.TableViewDto { Id = 5, BranchId = 9 });
        _productServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(product);

        // Act
        await _service.ConfirmAsync(1);

        // Assert
        _invoiceServiceMock.Verify(
            s => s.CreateInvoiceAsync(9, It.Is<List<InvoiceOutflowLineDto>>(l => l.Count == 1 && l[0].ProductId == 1 && l[0].Quantity == 2), null),
            Times.Once);
    }

    [Fact]
    public async Task DoesNotDeductInventory_WhenAlreadyConfirmed()
    {
        // Arrange
        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new OrderDetail { Id = 1, OrderId = 1, ProductId = 1, Quantity = 2, Status = "Confirmed" });
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(1, "Confirmed")).ReturnsAsync(true);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Order { Id = 1, TableId = 5, Status = "Active" });

        // Act
        await _service.ConfirmAsync(1);

        // Assert
        _invoiceServiceMock.Verify(
            s => s.CreateInvoiceAsync(It.IsAny<long>(), It.IsAny<List<InvoiceOutflowLineDto>>(), It.IsAny<long?>()),
            Times.Never);
    }
}
