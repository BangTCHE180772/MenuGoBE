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
    private readonly HashSet<long> _warnedReservationIds = new HashSet<long>();
    private readonly HashSet<long> _lateWarnedReservationIds = new HashSet<long>();

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
                bool isPeakHour = false;

                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var now = DateTime.UtcNow;
                    var localNow = now.AddHours(7); // Vietnam time (UTC+7)

                    // Peak hours for Escargot/Seafood restaurant: 16:00-18:00 and 20:00-22:00
                    int hour = localNow.Hour;
                    int min = localNow.Minute;
                    isPeakHour = (hour >= 16 && hour < 18) || (hour == 18 && min == 0) ||
                                 (hour >= 20 && hour < 22) || (hour == 22 && min == 0);

                    var expiredTime = now.AddMinutes(-30);
                    var lockThreshold = now.AddMinutes(30);

                    // 1. Process expired reservations (30 minutes late)
                    var expiredList = await db.Reservations
                        .Include(r => r.Order).ThenInclude(o => o!.Table)
                        .Where(r => r.Status == "Pending" && r.Order != null && r.Order.Table != null && r.ReservationTime <= expiredTime)
                        .ToListAsync(stoppingToken);

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

                    // 2. Process upcoming reservations that need locking (<= 30 mins away)
                    var toLockList = await db.Reservations
                        .Include(r => r.Order).ThenInclude(o => o!.Table)
                        .Where(r => r.Status == "Pending" && r.Order != null && r.Order.Table != null 
                                 && r.Order.Table.Status == "Empty" 
                                 && r.ReservationTime <= lockThreshold && r.ReservationTime > expiredTime)
                        .ToListAsync(stoppingToken);

                    foreach (var res in toLockList)
                    {
                        var table = res.Order!.Table!;
                        table.Status = "Reserved";
                        db.Tables.Update(table);
                        updatedTableIds.Add(table.Id);
                    }

                    // 3. Process conflict warnings: <= 30 mins away, table Occupied
                    var warnCandidates = await db.Reservations
                        .Include(r => r.Order).ThenInclude(o => o!.Table)
                        .Where(r => r.Status == "Pending" && r.Order != null && r.Order.Table != null 
                                 && r.Order.Table.Status == "Occupied" 
                                 && r.ReservationTime <= lockThreshold && r.ReservationTime > expiredTime)
                        .ToListAsync(stoppingToken);

                    var toWarnList = warnCandidates.Where(r => !_warnedReservationIds.Contains(r.Id)).ToList();

                    foreach (var warnRes in toWarnList)
                    {
                        var table = warnRes.Order!.Table!;
                        var area = await db.Areas.FirstOrDefaultAsync(a => a.Id == table.AreaId);
                        long branchId = area?.BranchId ?? 0;

                        // Send SignalR event
                        await _hubContext.Clients.All.SendAsync("TableConflictWarning", new
                        {
                            ReservationId = warnRes.Id,
                            TableId = table.Id,
                            TableName = table.Name,
                            BranchId = branchId,
                            ReservationTime = warnRes.ReservationTime,
                            Message = $"Bàn {table.Name} đã được đặt lúc {warnRes.ReservationTime:HH:mm} nhưng hiện vẫn đang có khách ngồi! Vui lòng sắp xếp bàn dự phòng."
                        });
                        _warnedReservationIds.Add(warnRes.Id);
                    }

                    // 4. Process late warnings: 15 mins late
                    var lateWarningThreshold = now.AddMinutes(-15);
                    var lateWarnCandidates = await db.Reservations
                        .Include(r => r.Order).ThenInclude(o => o!.Table)
                        .Where(r => r.Status == "Pending" && r.ReservationTime <= lateWarningThreshold && r.ReservationTime > expiredTime)
                        .ToListAsync(stoppingToken);

                    var toLateWarnList = lateWarnCandidates.Where(r => !_lateWarnedReservationIds.Contains(r.Id)).ToList();

                    foreach (var lateWarn in toLateWarnList)
                    {
                        var table = lateWarn.Order?.Table;
                        long tableId = table?.Id ?? 0;
                        string tableName = table?.Name ?? "Không rõ";

                        await _hubContext.Clients.All.SendAsync("ReservationLateWarning", new
                        {
                            ReservationId = lateWarn.Id,
                            TableId = tableId,
                            TableName = tableName,
                            ReservationTime = lateWarn.ReservationTime,
                            Message = $"Khách đặt bàn {tableName} lúc {lateWarn.ReservationTime:HH:mm} đã trễ hơn 15 phút. Bạn có muốn gia hạn thời gian giữ bàn không?"
                        });
                        _lateWarnedReservationIds.Add(lateWarn.Id);
                    }

                    if (expiredList.Any() || toLockList.Any())
                    {
                        await db.SaveChangesAsync(stoppingToken);
                        shouldLoopImmediately = true;
                    }
                    else
                    {
                        if (isPeakHour)
                        {
                            // Fixed polling interval during peak hours to batch events and prevent Event Storms
                            delay = TimeSpan.FromMinutes(5);
                        }
                        else
                        {
                            // Event-driven mode for off-peak: compute exact delay to the next event
                            var nextExpiration = await db.Reservations
                                .Where(r => r.Status == "Pending" && r.ReservationTime > expiredTime)
                                .OrderBy(r => r.ReservationTime)
                                .Select(r => (DateTime?)r.ReservationTime.AddMinutes(30))
                                .FirstOrDefaultAsync(stoppingToken);

                            var nextLockWarnCandidates = await db.Reservations
                                .Where(r => r.Status == "Pending" && r.Order != null && r.Order.Table != null
                                         && (r.Order.Table.Status == "Empty" || r.Order.Table.Status == "Occupied")
                                         && r.ReservationTime > lockThreshold)
                                .OrderBy(r => r.ReservationTime)
                                .Take(20)
                                .Include(r => r.Order).ThenInclude(o => o!.Table)
                                .ToListAsync(stoppingToken);

                            var nextLockWarn = nextLockWarnCandidates
                                .Where(r => r.Order!.Table!.Status == "Empty" || !_warnedReservationIds.Contains(r.Id))
                                .Select(r => (DateTime?)r.ReservationTime.AddMinutes(-30))
                                .FirstOrDefault();

                            var nextLateWarnCandidates = await db.Reservations
                                .Where(r => r.Status == "Pending" && r.ReservationTime > lateWarningThreshold)
                                .OrderBy(r => r.ReservationTime)
                                .Take(20)
                                .ToListAsync(stoppingToken);

                            var nextLateWarn = nextLateWarnCandidates
                                .Where(r => !_lateWarnedReservationIds.Contains(r.Id))
                                .Select(r => (DateTime?)r.ReservationTime.AddMinutes(15))
                                .FirstOrDefault();

                            List<DateTime> events = new List<DateTime>();
                            if (nextExpiration.HasValue) events.Add(nextExpiration.Value);
                            if (nextLockWarn.HasValue) events.Add(nextLockWarn.Value);
                            if (nextLateWarn.HasValue) events.Add(nextLateWarn.Value);

                            if (events.Any())
                            {
                                var nextEventTime = events.Min();
                                if (nextEventTime > now) delay = nextEventTime - now;
                                else delay = TimeSpan.FromSeconds(30);
                            }
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

                // Minimum sleep to prevent tight loops
                if (delay != Timeout.InfiniteTimeSpan && delay.TotalMinutes < 1)
                {
                    delay = TimeSpan.FromMinutes(1);
                }

                var delayTask = Task.Delay(delay, stoppingToken);

                if (isPeakHour)
                {
                    // Peak hour: Ignore manual signals to enforce batch processing
                    await delayTask;
                }
                else
                {
                    // Off-peak: Wake up immediately on manual signal (new reservation)
                    var signalTask = _signalService.WaitAsync(stoppingToken);
                    await Task.WhenAny(delayTask, signalTask);
                }
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
