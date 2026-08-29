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

public class ProcessExpiredReservationsTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;

    public ProcessExpiredReservationsTests()
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
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
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
    public async Task CancelsReservation_When30MinutesLate()
    {
        // Arrange
        using (var seedContext = new AppDbContext(_dbOptions))
        {
            var table = new Table { Id = 1, Name = "Bàn 1", Status = "Reserved" };
            var order = new Order { Id = 10, TableId = 1, Table = table, Status = "Reserved" };
            var reservation = new Reservation
            {
                Id = 100,
                Status = "Pending",
                Order = order,
                OrderId = 10,
                ReservationTime = DateTime.UtcNow.AddMinutes(-35) // Expired (> 30 mins late)
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
        using (var assertContext = new AppDbContext(_dbOptions))
        {
            var updatedRes = await assertContext.Reservations.FindAsync(100L);
            var updatedTable = await assertContext.Tables.FindAsync(1L);

            Assert.NotNull(updatedRes);
            Assert.Equal("Cancelled", updatedRes.Status);
            Assert.NotNull(updatedTable);
            Assert.Equal("Empty", updatedTable.Status);
        }
    }

    [Fact]
    public async Task DoesNotCancel_WhenWithin30MinutesLate()
    {
        // Arrange
        using (var seedContext = new AppDbContext(_dbOptions))
        {
            var table = new Table { Id = 2, Name = "Bàn 2", Status = "Reserved" };
            var order = new Order { Id = 20, TableId = 2, Table = table, Status = "Reserved" };
            var reservation = new Reservation
            {
                Id = 200,
                Status = "Pending",
                Order = order,
                OrderId = 20,
                ReservationTime = DateTime.UtcNow.AddMinutes(-10) // Only 10 mins late (not expired yet)
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
        using (var assertContext = new AppDbContext(_dbOptions))
        {
            var updatedRes = await assertContext.Reservations.FindAsync(200L);
            Assert.NotNull(updatedRes);
            Assert.Equal("Pending", updatedRes.Status);
        }
    }
}
