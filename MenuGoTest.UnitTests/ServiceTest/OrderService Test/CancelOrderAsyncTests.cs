using MenuGoBE.Dtos.Order;
using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderServiceTest;

public class CancelOrderAsyncTests : OrderServiceTestBase
{
    [Fact]
    public async Task ReturnsTrue_WhenAllItemsWaiting()
    {
        var order = new Order
        {
            Id = 1, TableId = 10, Status = "Active",
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { Id = 1, Status = "Confirmed", CookingStatus = "Waiting" },
                new OrderDetail { Id = 2, Status = "Confirmed", CookingStatus = "Waiting" }
            }
        };
        var table = new Table { Id = 10, Name = "Bàn 10", Status = "Occupied", AreaId = 1 };

        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.CancelOrderAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenSomeItemsCooking()
    {
        var order = new Order
        {
            Id = 1, TableId = 10, Status = "Active",
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { Id = 1, Status = "Confirmed", CookingStatus = "Waiting" },
                new OrderDetail { Id = 2, Status = "Confirmed", CookingStatus = "Cooking" } // can't cancel
            }
        };
        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);

        var result = await _service.CancelOrderAsync(1);

        Assert.False(result);
    }

    [Fact]
    public async Task SetsTableToEmpty_WhenCancelled()
    {
        var order = new Order
        {
            Id = 1, TableId = 10, Status = "Active",
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { Id = 1, Status = "Confirmed", CookingStatus = "Waiting" }
            }
        };
        var table = new Table { Id = 10, Name = "Bàn 10", Status = "Occupied", AreaId = 1 };

        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _service.CancelOrderAsync(1);

        Assert.Equal("Empty", table.Status);
    }

    [Fact]
    public async Task CancelsAllOrderDetails()
    {
        var detail1 = new OrderDetail { Id = 1, Status = "Confirmed", CookingStatus = "Waiting" };
        var detail2 = new OrderDetail { Id = 2, Status = "Confirmed", CookingStatus = "Waiting" };
        var order = new Order { Id = 1, TableId = 10, Status = "Active", OrderDetails = new List<OrderDetail> { detail1, detail2 } };
        var table = new Table { Id = 10, Status = "Occupied", AreaId = 1 };

        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _service.CancelOrderAsync(1);

        Assert.Equal("Cancelled", detail1.Status);
        Assert.Equal("Cancelled", detail2.Status);
        Assert.Equal("Cancelled", order.Status);
    }

    [Fact]
    public async Task ReturnsFalse_WhenOrderNotFound()
    {
        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(999)).ReturnsAsync((Order?)null);

        var result = await _service.CancelOrderAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task AllowsCancel_WhenSomeItemsAlreadyCancelled()
    {
        // Items that are already cancelled should be ignored in the cooking check
        var order = new Order
        {
            Id = 1, TableId = 10, Status = "Active",
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { Id = 1, Status = "Cancelled", CookingStatus = "Cooking" }, // already cancelled → ignore
                new OrderDetail { Id = 2, Status = "Confirmed", CookingStatus = "Waiting" }  // ok to cancel
            }
        };
        var table = new Table { Id = 10, Status = "Occupied", AreaId = 1 };

        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.CancelOrderAsync(1);

        Assert.True(result);
    }
}
