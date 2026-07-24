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
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly HashSet<long> _warnedReservationIds = new HashSet<long>();
    private readonly HashSet<long> _lateWarnedReservationIds = new HashSet<long>();

    public ReservationMonitorService(
        IServiceProvider serviceProvider, 
        IHubContext<NotificationHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                List<long> updatedTableIds = new List<long>();

                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var now = DateTime.UtcNow;

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

                        var childOrders = await db.Orders.Include(o => o.Table)
                            .Where(o => o.FatherId == expired.Order.Id).ToListAsync(stoppingToken);
                        
                        foreach(var child in childOrders)
                        {
                            if(child.Table != null && child.Table.Status == "Reserved")
                            {
                                child.Table.Status = "Empty";
                                db.Tables.Update(child.Table);
                                updatedTableIds.Add(child.Table.Id);
                            }
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

                        var childOrders = await db.Orders.Include(o => o.Table)
                            .Where(o => o.FatherId == res.Order.Id).ToListAsync(stoppingToken);
                        
                        foreach(var child in childOrders)
                        {
                            if(child.Table != null && child.Table.Status == "Empty")
                            {
                                child.Table.Status = "Reserved";
                                db.Tables.Update(child.Table);
                                updatedTableIds.Add(child.Table.Id);
                            }
                        }
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
                    }
                }

                // Notify UI to refresh table statuses if any changes were made
                if (updatedTableIds.Any())
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveTableListUpdate", stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReservationMonitorService Error: {ex.Message}");
            }

            // Fixed 5-minute interval check
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
