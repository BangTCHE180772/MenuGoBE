using System.Reflection;
using MenuGoBE.Hubs;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.AutoCheckoutServiceTest;

public class ProcessAutoCheckoutsTests
{
    private readonly Mock<IWorkScheduleRepository> _workScheduleRepoMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<AutoCheckoutService>> _loggerMock;
    private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;

    public ProcessAutoCheckoutsTests()
    {
        _workScheduleRepoMock = new Mock<IWorkScheduleRepository>();
        _loggerMock = new Mock<ILogger<AutoCheckoutService>>();
        _hubContextMock = new Mock<IHubContext<NotificationHub>>();

        var orderAssignmentServiceMock = new Mock<MenuGoBE.Interface.Services.IOrderAssignmentService>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IWorkScheduleRepository)))
            .Returns(_workScheduleRepoMock.Object);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(MenuGoBE.Interface.Services.IOrderAssignmentService)))
            .Returns(orderAssignmentServiceMock.Object);

        var serviceScopeMock = new Mock<IServiceScope>();
        serviceScopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeFactoryMock.Setup(sf => sf.CreateScope()).Returns(serviceScopeMock.Object);
    }

    [Fact]
    public async Task ForcedCheckout_When120MinutesPastEndTime_NoNextShift()
    {
        // Arrange
        var localNow = DateTime.UtcNow.AddHours(7);
        var today = DateOnly.FromDateTime(localNow);

        var pastShift = new Shift
        {
            Id = 1,
            StartTime = TimeOnly.FromDateTime(localNow.AddHours(-5)),
            EndTime = TimeOnly.FromDateTime(localNow.AddMinutes(-130)) // Over 2 hours ago
        };

        var openSchedule = new WorkSchedule
        {
            Id = 100,
            AccountId = 1,
            BranchId = 1,
            WorkDate = today,
            Shift = pastShift,
            CheckInAt = DateTime.UtcNow.AddMinutes(-50),
            Status = "Working"
        };

        _workScheduleRepoMock.Setup(r => r.GetOpenSchedulesAsync())
            .ReturnsAsync(new List<WorkSchedule> { openSchedule });
        _workScheduleRepoMock.Setup(r => r.GetPendingSchedulesByAccountIdsAsync(It.IsAny<List<long>>()))
            .ReturnsAsync(new List<WorkSchedule>());
        _workScheduleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<WorkSchedule>()))
            .Returns(Task.CompletedTask);
        _workScheduleRepoMock.Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var autoCheckoutService = new AutoCheckoutService(_scopeFactoryMock.Object, _loggerMock.Object, _hubContextMock.Object);

        // Act - invoke private ProcessAutoCheckoutsAsync directly via Reflection
        var method = typeof(AutoCheckoutService).GetMethod("ProcessAutoCheckoutsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        await (Task)method.Invoke(autoCheckoutService, null)!;

        // Assert
        Assert.Equal("Completed", openSchedule.Status);
        Assert.NotNull(openSchedule.CheckOutAt);
        _workScheduleRepoMock.Verify(r => r.UpdateAsync(openSchedule), Times.Once);
        _workScheduleRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AutoTransitions_WhenNextShiftExists()
    {
        // Arrange
        var localNow = DateTime.UtcNow.AddHours(7);
        var today = DateOnly.FromDateTime(localNow);

        var currentShift = new Shift
        {
            Id = 1,
            StartTime = TimeOnly.FromDateTime(localNow.AddMinutes(-30)),
            EndTime = TimeOnly.FromDateTime(localNow.AddMinutes(-5)) // Just ended
        };

        var nextShift = new Shift
        {
            Id = 2,
            StartTime = TimeOnly.FromDateTime(localNow.AddMinutes(-5)), // Starts right at current shift end
            EndTime = TimeOnly.FromDateTime(localNow.AddMinutes(30))
        };

        var currentSchedule = new WorkSchedule
        {
            Id = 100,
            AccountId = 1,
            BranchId = 1,
            WorkDate = today,
            Shift = currentShift,
            CheckInAt = DateTime.UtcNow.AddMinutes(-30),
            Status = "Working"
        };

        var nextSchedule = new WorkSchedule
        {
            Id = 101,
            AccountId = 1,
            BranchId = 1,
            WorkDate = today,
            Shift = nextShift,
            Status = "Pending"
        };

        _workScheduleRepoMock.Setup(r => r.GetOpenSchedulesAsync())
            .ReturnsAsync(new List<WorkSchedule> { currentSchedule });
        _workScheduleRepoMock.Setup(r => r.GetPendingSchedulesByAccountIdsAsync(new List<long> { 1 }))
            .ReturnsAsync(new List<WorkSchedule> { nextSchedule });
        _workScheduleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<WorkSchedule>()))
            .Returns(Task.CompletedTask);
        _workScheduleRepoMock.Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var autoCheckoutService = new AutoCheckoutService(_scopeFactoryMock.Object, _loggerMock.Object, _hubContextMock.Object);

        // Act
        var method = typeof(AutoCheckoutService).GetMethod("ProcessAutoCheckoutsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        await (Task)method.Invoke(autoCheckoutService, null)!;

        // Assert
        Assert.Equal("Completed", currentSchedule.Status);
        Assert.Equal("Working", nextSchedule.Status);
        Assert.NotNull(nextSchedule.CheckInAt);
        _workScheduleRepoMock.Verify(r => r.UpdateAsync(currentSchedule), Times.Once);
        _workScheduleRepoMock.Verify(r => r.UpdateAsync(nextSchedule), Times.Once);
    }
}
