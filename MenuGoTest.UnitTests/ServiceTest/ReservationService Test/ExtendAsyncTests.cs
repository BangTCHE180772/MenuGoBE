using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.ReservationServiceTest;

public class ExtendAsyncTests : ReservationServiceTestBase
{
    [Fact]
    public async Task ReturnsTrue_WhenPending()
    {
        var originalTime = DateTime.UtcNow.AddHours(1);
        var reservation = new Reservation { Id = 1, Status = "Pending", ReservationTime = originalTime };

        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _reservationRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Reservation>())).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.ExtendAsync(1, 30);

        Assert.True(result);
        Assert.Equal(originalTime.AddMinutes(30), reservation.ReservationTime);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNotPending()
    {
        var reservation = new Reservation { Id = 1, Status = "Completed" };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);

        var result = await _service.ExtendAsync(1, 30);

        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNotExists()
    {
        _reservationRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Reservation?)null);

        var result = await _service.ExtendAsync(999, 30);

        Assert.False(result);
    }

    [Fact]
    public async Task TriggersSignalService()
    {
        var reservation = new Reservation { Id = 1, Status = "Pending", ReservationTime = DateTime.UtcNow.AddHours(1) };
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _reservationRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Reservation>())).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _service.ExtendAsync(1, 15);

        _signalServiceMock.Verify(s => s.TriggerSignal(), Times.Once);
    }
}
