using System.Reflection;
using MenuGoBE.Hubs;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Service;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.AutoCheckoutServiceTest;

public class ExecuteAsyncTests
{
    private readonly Mock<IWorkScheduleRepository> _workScheduleRepoMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<AutoCheckoutService>> _loggerMock;
    private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;

    public ExecuteAsyncTests()
    {
        _workScheduleRepoMock = new Mock<IWorkScheduleRepository>();
        _loggerMock = new Mock<ILogger<AutoCheckoutService>>();
        _hubContextMock = new Mock<IHubContext<NotificationHub>>();

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IWorkScheduleRepository)))
            .Returns(_workScheduleRepoMock.Object);

        var serviceScopeMock = new Mock<IServiceScope>();
        serviceScopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeFactoryMock.Setup(sf => sf.CreateScope()).Returns(serviceScopeMock.Object);
    }

    [Fact]
    public async Task ServiceStopsGracefully_OnCancellationToken()
    {
        // Arrange
        _workScheduleRepoMock.Setup(r => r.GetOpenSchedulesAsync())
            .ReturnsAsync(new List<MenuGoBE.Models.WorkSchedule>());

        var autoCheckoutService = new AutoCheckoutService(_scopeFactoryMock.Object, _loggerMock.Object, _hubContextMock.Object);
        using var cts = new CancellationTokenSource();

        var method = typeof(AutoCheckoutService).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act - cancel token right away
        cts.Cancel();
        var task = (Task)method.Invoke(autoCheckoutService, new object[] { cts.Token })!;
        await task;

        // Assert - completed gracefully without unhandled exception
        Assert.True(task.IsCompleted);
    }
}
