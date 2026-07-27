using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.ReservationServiceTest;

public class CancelAsyncTests : ReservationServiceTestBase
{
    [Fact]
    public async Task ReturnsTrue_WhenPending()
    {
        var reservation = new Reservation { Id = 1, Status = "Pending", OrderId = 10 };
        var order = new Order { Id = 10, TableId = 1, Status = "Reserved", OrderDetails = new List<OrderDetail>() };
        var table = new Table { Id = 1, Status = "Reserved", AreaId = 1 };

        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(10)).ReturnsAsync((Order?)null);
        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(new List<OrderDetail>());
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(10)).ReturnsAsync(new List<Order>());
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Table>())).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Reservation>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.CancelAsync(1);

        Assert.True(result);
        Assert.Equal("Cancelled", reservation.Status);
        Assert.Equal("Cancelled", order.Status);
        Assert.Equal("Empty", table.Status);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNotPending()
    {
        var reservation = new Reservation { Id = 1, Status = "Completed" };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);

        var result = await _service.CancelAsync(1);

        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNotExists()
    {
        _reservationRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Reservation?)null);

        var result = await _service.CancelAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task CancelsPreOrderDetails()
    {
        var detail = new OrderDetail { Id = 1, OrderId = 10, Status = "PreOrder" };
        var reservation = new Reservation { Id = 1, Status = "Pending", OrderId = 10 };
        var order = new Order { Id = 10, TableId = 1, Status = "Reserved", OrderDetails = new List<OrderDetail> { detail } };
        var table = new Table { Id = 1, Status = "Reserved", AreaId = 1 };

        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(10)).ReturnsAsync((Order?)null);
        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(new List<OrderDetail> { detail });
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(1, "Cancelled")).ReturnsAsync(true);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(10)).ReturnsAsync(new List<Order>());
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Table>())).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Reservation>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _service.CancelAsync(1);

        _detailRepoMock.Verify(r => r.UpdateStatusAsync(1, "Cancelled"), Times.Once);
    }

    [Fact]
    public async Task CancelsChildOrders_AndReleasesChildTables()
    {
        var reservation = new Reservation { Id = 1, Status = "Pending", OrderId = 10 };
        var order = new Order { Id = 10, TableId = 1, Status = "Reserved", OrderDetails = new List<OrderDetail>() };
        var table = new Table { Id = 1, Status = "Reserved", AreaId = 1 };
        var childOrder = new Order { Id = 11, TableId = 2, Status = "Reserved", FatherId = 10, OrderDetails = new List<OrderDetail>() };
        var childTable = new Table { Id = 2, Status = "Reserved", AreaId = 1 };

        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(10)).ReturnsAsync((Order?)null);
        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _tableRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(childTable);
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(new List<OrderDetail>());
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(10)).ReturnsAsync(new List<Order> { childOrder });
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Table>())).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Reservation>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _service.CancelAsync(1);

        Assert.Equal("Cancelled", childOrder.Status);
        Assert.Equal("Empty", childTable.Status);
    }
}
