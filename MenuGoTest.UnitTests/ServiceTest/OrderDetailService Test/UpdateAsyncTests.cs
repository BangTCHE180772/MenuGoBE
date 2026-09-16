using AutoMapper;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderDetailServiceTest;

public class UpdateAsyncTests
{
    private readonly Mock<IOrderDetailRepository> _detailRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly IMapper _mapper;
    private readonly OrderDetailService _service;

    public UpdateAsyncTests()
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
    public async Task ReturnsTrue_WhenExists()
    {
        // Arrange
        var existing = new OrderDetail { Id = 1, OrderId = 10, ProductId = 1, Quantity = 2, Price = 50000, Status = "Confirmed" };
        var dto = new OrderDetailUpdateDto { Id = 1, OrderId = 10, ProductId = 1, Quantity = 5, Price = 50000, Status = "Confirmed" };
        var order = new Order { Id = 10, TableId = 1, Status = "Active", TotalAmount = 100000 };

        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _detailRepoMock.Setup(r => r.UpdateAsync(It.IsAny<OrderDetail>())).Returns(Task.CompletedTask);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(new List<OrderDetail>
        {
            new OrderDetail { Id = 1, OrderId = 10, Quantity = 5, Price = 50000, Status = "Confirmed" }
        });
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNotExists()
    {
        // Arrange
        var dto = new OrderDetailUpdateDto { Id = 999, OrderId = 10, ProductId = 1, Quantity = 1, Price = 10000 };
        _detailRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((OrderDetail?)null);

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RecalculatesOrderTotal_AfterUpdate()
    {
        // Arrange
        var existing = new OrderDetail { Id = 1, OrderId = 10, ProductId = 1, Quantity = 2, Price = 50000, Status = "Confirmed" };
        var dto = new OrderDetailUpdateDto { Id = 1, OrderId = 10, ProductId = 1, Quantity = 4, Price = 50000, Status = "Confirmed" };
        var order = new Order { Id = 10, TableId = 1, Status = "Active", TotalAmount = 100000 };

        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _detailRepoMock.Setup(r => r.UpdateAsync(It.IsAny<OrderDetail>())).Returns(Task.CompletedTask);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(new List<OrderDetail>
        {
            new OrderDetail { Id = 1, OrderId = 10, Quantity = 4, Price = 50000, Status = "Confirmed" }
        });
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.UpdateAsync(dto);

        // Assert - 4 * 50000 = 200000
        _orderRepoMock.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.TotalAmount == 200000)), Times.Once);
    }
}

