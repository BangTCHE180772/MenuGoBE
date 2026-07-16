using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MenuGoBE.Data;
using MenuGoBE.Interface.Services;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using MenuGoBE.Hubs;

namespace MenuGoBE.Service;

public class ReservationMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReservationSignalService _signalService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public ReservationMonitorService(
        IServiceProvider serviceProvider, 
        IReservationSignalService signalService,
        IHubContext<NotificationHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _signalService = signalService;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                TimeSpan delay = Timeout.InfiniteTimeSpan;
                bool shouldLoopImmediately = false;
                List<long> updatedTableIds = new List<long>();

                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var now = DateTime.UtcNow;

                    var pendingReservations = await db.Reservations
                        .Include(r => r.Order)
                            .ThenInclude(o => o.Table)
                        .Where(r => r.Status == "Pending" && r.Order != null && r.Order.Table != null)
                        .ToListAsync(stoppingToken);

                    // 1. Process expired reservations (30 minutes late)
                    var expiredList = pendingReservations.Where(r => now >= r.ReservationTime.AddMinutes(30)).ToList();
                    foreach (var expired in expiredList)
                    {
                        expired.Status = "Cancelled";
                        db.Reservations.Update(expired);

                        var table = expired.Order!.Table;
                        if (table != null && table.Status == "Reserved")
                        {
                            table.Status = "Empty";
                            db.Tables.Update(table);
                            updatedTableIds.Add(table.Id);
                        }
                    }

                    // 2. Process upcoming reservations that need locking (<= 2 hours away)
                    var toLockList = pendingReservations
                        .Where(r => r.Order!.Table!.Status == "Empty" && now >= r.ReservationTime.AddHours(-2) && now < r.ReservationTime.AddMinutes(30))
                        .ToList();

                    foreach (var res in toLockList)
                    {
                        var table = res.Order!.Table!;
                        table.Status = "Reserved";
                        db.Tables.Update(table);
                        updatedTableIds.Add(table.Id);
                    }

                    if (expiredList.Any() || toLockList.Any())
                    {
                        await db.SaveChangesAsync(stoppingToken);
                        shouldLoopImmediately = true;
                    }
                    else if (pendingReservations.Any())
                    {
                        // Calculate delay to the NEXT event
                        // An event is either a table needing a lock (ReservationTime - 2h)
                        // OR a reservation expiring (ReservationTime + 30m)
                        
                        var upcomingLocks = pendingReservations
                            .Where(r => r.Order!.Table!.Status == "Empty" && r.ReservationTime.AddHours(-2) > now)
                            .Select(r => r.ReservationTime.AddHours(-2));

                        var upcomingExpirations = pendingReservations
                            .Where(r => r.ReservationTime.AddMinutes(30) > now)
                            .Select(r => r.ReservationTime.AddMinutes(30));

                        var allFutureEvents = upcomingLocks.Concat(upcomingExpirations).ToList();
                        
                        if (allFutureEvents.Any())
                        {
                            var nextEventTime = allFutureEvents.Min();
                            delay = nextEventTime - now;
                        }
                    }
                }

                if (shouldLoopImmediately)
                {
                    // Notify UI to refresh table statuses
                    foreach (var tableId in updatedTableIds.Distinct())
                    {
                        await _hubContext.Clients.All.SendAsync("ReceiveNotification", new 
                        {
                            tableId = tableId,
                            type = "ReservationUpdate",
                            message = "Table status updated by reservation monitor."
                        }, stoppingToken);
                    }
                    continue;
                }

                // If delay is somehow negative or zero, just yield to prevent tight loop
                if (delay != Timeout.InfiniteTimeSpan && delay.TotalMilliseconds <= 0)
                {
                    delay = TimeSpan.FromMilliseconds(100);
                }

                // Wait for the calculated delay, OR until a new reservation wakes us up
                var delayTask = Task.Delay(delay, stoppingToken);
                var signalTask = _signalService.WaitAsync(stoppingToken);

                await Task.WhenAny(delayTask, signalTask);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReservationMonitorService Error: {ex.Message}");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
