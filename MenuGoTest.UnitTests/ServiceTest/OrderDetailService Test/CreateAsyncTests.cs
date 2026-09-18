using AutoMapper;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderDetailServiceTest;

public class CreateAsyncTests
{
    private readonly Mock<IOrderDetailRepository> _detailRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly IMapper _mapper;
    private readonly OrderDetailService _service;

    public CreateAsyncTests()
    {
        _detailRepoMock = new Mock<IOrderDetailRepository>();
        _orderRepoMock = new Mock<IOrderRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new OrderDetailService(_detailRepoMock.Object, _orderRepoMock.Object, _mapper, new Mock<MenuGoBE.Interface.Repository.ITableRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IProductRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IAreaRepository>().Object, new Mock<MenuGoBE.Interface.Services.IKitchenService>().Object, new Mock<MenuGoBE.Interface.Repository.IBInventoryRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IRecipesDetailedRepository>().Object, new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MenuGoBE.Hubs.NotificationHub>>().Object, new Mock<MenuGoBE.Interface.Services.Document.IDocumentService>().Object, new Mock<MenuGoBE.Interface.Services.ILeftoverRecordService>().Object, new Mock<MenuGoBE.Interface.Services.IPromotionService>().Object);
    }

    [Fact]
    public async Task ReturnsDto_WhenValid()
    {
        // Arrange
        var order = new Order { Id = 10, TableId = 1, Status = "Active", TotalAmount = 0 };
        var dto = new OrderDetailCreateDto { OrderId = 10, ProductId = 1, Quantity = 2, Price = 50000, Status = "Confirmed", CookingStatus = "Waiting" };

        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _detailRepoMock.Setup(r => r.CreateAsync(It.IsAny<OrderDetail>())).Returns(Task.CompletedTask);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(new List<OrderDetail>
        {
            new OrderDetail { Id = 1, OrderId = 10, ProductId = 1, Quantity = 2, Price = 50000, Status = "Confirmed" }
        });
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.OrderId);
    }

    [Fact]
    public async Task ThrowsKeyNotFound_WhenOrderNotExists()
    {
        // Arrange
        var dto = new OrderDetailCreateDto { OrderId = 999, ProductId = 1, Quantity = 1, Price = 10000 };
        _orderRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Order?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task RecalculatesOrderTotal_AfterCreate()
    {
        // Arrange
        var order = new Order { Id = 10, TableId = 1, Status = "Active", TotalAmount = 0 };
        var dto = new OrderDetailCreateDto { OrderId = 10, ProductId = 1, Quantity = 3, Price = 40000, Status = "Confirmed", CookingStatus = "Waiting" };

        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _detailRepoMock.Setup(r => r.CreateAsync(It.IsAny<OrderDetail>())).Returns(Task.CompletedTask);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(new List<OrderDetail>
        {
            new OrderDetail { Id = 1, OrderId = 10, Quantity = 3, Price = 40000, Status = "Confirmed" }
        });
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.CreateAsync(dto);

        // Assert - TotalAmount = 3 * 40000 = 120000
        _orderRepoMock.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.TotalAmount == 120000)), Times.Once);
    }

    [Fact]
    public async Task CallsCreateAndSave()
    {
        // Arrange
        var order = new Order { Id = 10, TableId = 1, Status = "Active", TotalAmount = 0 };
        var dto = new OrderDetailCreateDto { OrderId = 10, ProductId = 1, Quantity = 1, Price = 10000, Status = "Confirmed" };

        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _detailRepoMock.Setup(r => r.CreateAsync(It.IsAny<OrderDetail>())).Returns(Task.CompletedTask);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(new List<OrderDetail>());
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.CreateAsync(dto);

        // Assert
        _detailRepoMock.Verify(r => r.CreateAsync(It.IsAny<OrderDetail>()), Times.Once);
        _detailRepoMock.Verify(r => r.SaveChangesAsync(), Times.AtLeastOnce);
    }
}

