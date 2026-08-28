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

public class ProcessTableLockTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;

    public ProcessTableLockTests()
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
    public async Task LocksTable_WhenEmptyAndWithin30Minutes()
    {
        // Arrange
        using (var seedContext = new AppDbContext(_dbOptions))
        {
            var table = new Table { Id = 2, Name = "Bàn 2", Status = "Empty" };
            var order = new Order { Id = 20, TableId = 2, Table = table, Status = "Reserved" };
            var reservation = new Reservation
            {
                Id = 200,
                Status = "Pending",
                Order = order,
                OrderId = 20,
                ReservationTime = DateTime.UtcNow.AddMinutes(15) // Within 30 mins away
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
            var updatedTable = await assertContext.Tables.FindAsync(2L);
            Assert.NotNull(updatedTable);
            Assert.Equal("Reserved", updatedTable.Status);
        }
    }

    [Fact]
    public async Task DoesNotLock_WhenBeyond30Minutes()
    {
        // Arrange
        using (var seedContext = new AppDbContext(_dbOptions))
        {
            var table = new Table { Id = 3, Name = "Bàn 3", Status = "Empty" };
            var order = new Order { Id = 30, TableId = 3, Table = table, Status = "Reserved" };
            var reservation = new Reservation
            {
                Id = 300,
                Status = "Pending",
                Order = order,
                OrderId = 30,
                ReservationTime = DateTime.UtcNow.AddHours(2) // Beyond 30 mins away
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
            var updatedTable = await assertContext.Tables.FindAsync(3L);
            Assert.NotNull(updatedTable);
            Assert.Equal("Empty", updatedTable.Status);
        }
    }
}
