using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.ReservationServiceTest;

public class ChangeTableAsyncTests : ReservationServiceTestBase
{
    [Fact]
    public async Task ReturnsTrue_WhenValidChange()
    {
        var reservation = new Reservation { Id = 1, Status = "Pending", OrderId = 10, ReservationTime = DateTime.UtcNow.AddHours(1) };
        var order = new Order { Id = 10, TableId = 1, Status = "Reserved" };
        var oldTable = new Table { Id = 1, Status = "Reserved", AreaId = 1 };
        var newTable = new Table { Id = 2, Status = "Empty", AreaId = 1 };

        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _tableRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(newTable);
        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(10)).ReturnsAsync((Order?)null);
        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(oldTable);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Table>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.ChangeTableAsync(1, 2);

        Assert.True(result);
        Assert.Equal("Empty", oldTable.Status);
        Assert.Equal(2, order.TableId);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNotPending()
    {
        var reservation = new Reservation { Id = 1, Status = "Completed" };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);

        var result = await _service.ChangeTableAsync(1, 2);

        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNewTableNotEmpty()
    {
        var reservation = new Reservation { Id = 1, Status = "Pending", OrderId = 10 };
        var newTable = new Table { Id = 2, Status = "Occupied", AreaId = 1 };

        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _tableRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(newTable);

        var result = await _service.ChangeTableAsync(1, 2);

        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNewTableNotExists()
    {
        var reservation = new Reservation { Id = 1, Status = "Pending" };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _tableRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Table?)null);

        var result = await _service.ChangeTableAsync(1, 999);

        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenReservationNotExists()
    {
        _reservationRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Reservation?)null);

        var result = await _service.ChangeTableAsync(999, 2);

        Assert.False(result);
    }
}
