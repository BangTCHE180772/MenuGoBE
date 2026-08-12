using AutoMapper;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderDetailServiceTest;

public class CancelOrderDetailAsyncTests
{
    private readonly Mock<IOrderDetailRepository> _detailRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly IMapper _mapper;
    private readonly OrderDetailService _service;

    public CancelOrderDetailAsyncTests()
    {
        _detailRepoMock = new Mock<IOrderDetailRepository>();
        _orderRepoMock = new Mock<IOrderRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new OrderDetailService(_detailRepoMock.Object, _orderRepoMock.Object, _mapper, new Mock<MenuGoBE.Interface.Repository.ITableRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IProductRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IAreaRepository>().Object, new Mock<MenuGoBE.Interface.Services.IKitchenService>().Object, new Mock<MenuGoBE.Interface.Repository.IBInventoryRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IRecipesDetailedRepository>().Object, new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MenuGoBE.Hubs.NotificationHub>>().Object, new Mock<MenuGoBE.Interface.Services.Document.IDocumentService>().Object);
    }

    [Fact]
    public async Task ReturnsTrue_WhenWaitingStatus()
    {
        // Arrange
        var detail = new OrderDetail { Id = 1, OrderId = 10, Quantity = 2, Price = 50000, Status = "Confirmed", CookingStatus = "Waiting" };
        var order = new Order { Id = 10, TableId = 1, Status = "Active", TotalAmount = 100000 };

        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(detail);
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(1, "Cancelled")).ReturnsAsync(true);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(new List<OrderDetail>
        {
            new OrderDetail { Id = 1, OrderId = 10, Quantity = 2, Price = 50000, Status = "Cancelled" }
        });
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CancelOrderDetailAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNotExists()
    {
        // Arrange
        _detailRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((OrderDetail?)null);

        // Act
        var result = await _service.CancelOrderDetailAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenAlreadyCancelled()
    {
        // Arrange
        var detail = new OrderDetail { Id = 1, OrderId = 10, Status = "Cancelled", CookingStatus = "Waiting" };
        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(detail);

        // Act
        var result = await _service.CancelOrderDetailAsync(1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenCookingInProgress()
    {
        // Arrange - CookingStatus = "Cooking" → không cho hủy
        var detail = new OrderDetail { Id = 1, OrderId = 10, Status = "Confirmed", CookingStatus = "Cooking" };
        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(detail);

        // Act
        var result = await _service.CancelOrderDetailAsync(1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenCookingStatusIsReady()
    {
        // Arrange - CookingStatus = "Ready" → cũng không cho hủy
        var detail = new OrderDetail { Id = 1, OrderId = 10, Status = "Confirmed", CookingStatus = "Ready" };
        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(detail);

        // Act
        var result = await _service.CancelOrderDetailAsync(1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RecalculatesOrderTotal_AfterCancel()
    {
        // Arrange - hủy 1 món, còn 1 món 30000 x 1
        var detail = new OrderDetail { Id = 1, OrderId = 10, Quantity = 2, Price = 50000, Status = "Confirmed", CookingStatus = "Waiting" };
        var order = new Order { Id = 10, TableId = 1, Status = "Active", TotalAmount = 130000 };

        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(detail);
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(1, "Cancelled")).ReturnsAsync(true);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(new List<OrderDetail>
        {
            new OrderDetail { Id = 1, OrderId = 10, Quantity = 2, Price = 50000, Status = "Cancelled" },
            new OrderDetail { Id = 2, OrderId = 10, Quantity = 1, Price = 30000, Status = "Confirmed" }
        });
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.CancelOrderDetailAsync(1);

        // Assert - only non-cancelled: 1 * 30000 = 30000
        _orderRepoMock.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.TotalAmount == 30000)), Times.Once);
    }
}
