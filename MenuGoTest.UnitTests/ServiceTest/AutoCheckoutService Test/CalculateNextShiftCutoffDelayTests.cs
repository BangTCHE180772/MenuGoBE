using System.Reflection;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.AutoCheckoutServiceTest;

public class CalculateNextShiftCutoffDelayTests
{
    private readonly Mock<IWorkScheduleRepository> _workScheduleRepoMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<AutoCheckoutService>> _loggerMock;

    public CalculateNextShiftCutoffDelayTests()
    {
        _workScheduleRepoMock = new Mock<IWorkScheduleRepository>();
        _loggerMock = new Mock<ILogger<AutoCheckoutService>>();

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IWorkScheduleRepository)))
            .Returns(_workScheduleRepoMock.Object);

        var serviceScopeMock = new Mock<IServiceScope>();
        serviceScopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeFactoryMock.Setup(sf => sf.CreateScope()).Returns(serviceScopeMock.Object);
    }

    [Fact]
    public async Task ReturnsDefault30Minutes_WhenNoOpenSchedules()
    {
        // Arrange
        _workScheduleRepoMock.Setup(r => r.GetOpenSchedulesAsync())
            .ReturnsAsync(new List<WorkSchedule>());

        var autoCheckoutService = new AutoCheckoutService(_scopeFactoryMock.Object, _loggerMock.Object);

        // Act
        var method = typeof(AutoCheckoutService).GetMethod("CalculateNextShiftCutoffDelayAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var task = (Task<TimeSpan>)method.Invoke(autoCheckoutService, null)!;
        var delay = await task;

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(30), delay);
    }

    [Fact]
    public async Task CalculatesCorrectDelay_ForNextShiftCutoff()
    {
        // Arrange
        var localNow = DateTime.UtcNow.AddHours(7);
        var today = DateOnly.FromDateTime(localNow);

        var futureShift = new Shift
        {
            Id = 1,
            StartTime = TimeOnly.FromDateTime(localNow.AddHours(-1)),
            EndTime = TimeOnly.FromDateTime(localNow.AddMinutes(20)) // 20 mins from now
        };

        var openSchedule = new WorkSchedule
        {
            Id = 100,
            AccountId = 1,
            BranchId = 1,
            WorkDate = today,
            Shift = futureShift,
            CheckInAt = DateTime.UtcNow.AddHours(-1),
            Status = "Working"
        };

        _workScheduleRepoMock.Setup(r => r.GetOpenSchedulesAsync())
            .ReturnsAsync(new List<WorkSchedule> { openSchedule });

        var autoCheckoutService = new AutoCheckoutService(_scopeFactoryMock.Object, _loggerMock.Object);

        // Act
        var method = typeof(AutoCheckoutService).GetMethod("CalculateNextShiftCutoffDelayAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var task = (Task<TimeSpan>)method.Invoke(autoCheckoutService, null)!;
        var delay = await task;

        // Assert - delay should be around 20 minutes
        Assert.True(delay.TotalMinutes <= 21 && delay.TotalMinutes >= 19);
    }
}
