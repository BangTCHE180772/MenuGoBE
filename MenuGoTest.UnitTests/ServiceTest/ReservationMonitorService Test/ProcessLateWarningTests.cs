using System.Reflection;
using MenuGoBE.Data;
using MenuGoBE.Hubs;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.ReservationMonitorServiceTest;

public class ProcessLateWarningTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;
    private readonly Mock<IClientProxy> _mockClientProxy;

    public ProcessLateWarningTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var services = new ServiceCollection();
        services.AddTransient(_ => new AppDbContext(_dbOptions));
        services.AddTransient(_ => new Mock<MenuGoBE.Interface.Services.IReservationService>().Object);
        services.AddTransient(_ => new Mock<MenuGoBE.Interface.Repository.INotificationRepository>().Object);
        _serviceProvider = services.BuildServiceProvider();

        _hubContextMock = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        _mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(mockClients.Object);
    }

    private async Task RunOneIterationAsync(ReservationMonitorService service)
    {
        using var cts = new CancellationTokenSource();
        var executeMethod = typeof(ReservationMonitorService).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(executeMethod);

        cts.CancelAfter(1500);

        try
        {
            var task = (Task)executeMethod.Invoke(service, new object[] { cts.Token })!;
            await Task.Delay(300);
            cts.Cancel();
            await task;
        }
        catch (Exception ex) when (ex is OperationCanceledException || ex.InnerException is OperationCanceledException)
        {
            // Expected on loop exit
        }
    }

    [Fact]
    public async Task SendsNotification_WhenReservationIs15MinutesLate()
    {
        // Arrange
        using (var seedContext = new AppDbContext(_dbOptions))
        {
            var table = new Table { Id = 4, Name = "Bàn 4", Status = "Reserved" };
            var order = new Order { Id = 40, TableId = 4, Table = table, Status = "Reserved" };
            var reservation = new Reservation
            {
                Id = 400,
                Status = "Pending",
                Order = order,
                OrderId = 40,
                ReservationTime = DateTime.UtcNow.AddMinutes(-20) // 20 mins late (> 15 mins)
            };

            seedContext.Tables.Add(table);
            seedContext.Orders.Add(order);
            seedContext.Reservations.Add(reservation);
            await seedContext.SaveChangesAsync();
        }

        var monitorService = new ReservationMonitorService(_serviceProvider, _hubContextMock.Object);

        // Act
        await RunOneIterationAsync(monitorService);

        // Assert - Verify SignalR hub notification sent
        _mockClientProxy.Verify(c => c.SendCoreAsync(
            "ReservationLateWarning",
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()
        ), Times.AtLeastOnce);
    }

    [Fact]
    public async Task DoesNotSendLateWarning_WhenNotYetLate()
    {
        // Arrange
        using (var seedContext = new AppDbContext(_dbOptions))
        {
            var table = new Table { Id = 5, Name = "Bàn 5", Status = "Reserved" };
            var order = new Order { Id = 50, TableId = 5, Table = table, Status = "Reserved" };
            var reservation = new Reservation
            {
                Id = 500,
                Status = "Pending",
                Order = order,
                OrderId = 50,
                ReservationTime = DateTime.UtcNow.AddMinutes(10) // Sắp tới, chưa trễ
            };

            seedContext.Tables.Add(table);
            seedContext.Orders.Add(order);
            seedContext.Reservations.Add(reservation);
            await seedContext.SaveChangesAsync();
        }

        var monitorService = new ReservationMonitorService(_serviceProvider, _hubContextMock.Object);

        // Act
        await RunOneIterationAsync(monitorService);

        // Assert
        _mockClientProxy.Verify(c => c.SendCoreAsync(
            "ReservationLateWarning",
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()
        ), Times.Never);
    }
}
