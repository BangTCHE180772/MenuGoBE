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

                        if (expired.Order != null)
                        {
                            expired.Order.Status = "Cancelled";
                            db.Orders.Update(expired.Order);
                        }

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
                            child.Status = "Cancelled";
                            db.Orders.Update(child);

                            if(child.Table != null && child.Table.Status == "Reserved")
                            {
                                child.Table.Status = "Empty";
                                db.Tables.Update(child.Table);
                                updatedTableIds.Add(child.Table.Id);
                            }
                        }
                    }

                    // 2. Auto-Assign (Cứu hộ đơn chưa xếp bàn)
                    var unassignedList = await db.Reservations
                        .Where(r => r.Status == "Pending" && r.OrderId == null && r.ReservationTime <= lockThreshold && r.ReservationTime > expiredTime)
                        .ToListAsync(stoppingToken);

                    var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();

                    foreach (var res in unassignedList)
                    {
                        if (res.BranchId.HasValue)
                        {
                            var emptyTables = await db.Tables
                                .Include(t => t.Area)
                                .Where(t => t.Area != null && t.Area.BranchId == res.BranchId.Value && (t.Status == "Empty" || t.Status == "Available"))
                                .ToListAsync(stoppingToken);
                            
                            var areaGroups = emptyTables.GroupBy(t => t.AreaId)
                                .Where(g => g.Count() >= res.TableCount)
                                .FirstOrDefault();

                            if (areaGroups != null)
                            {
                                var tableIdsToAssign = areaGroups.Take(res.TableCount).Select(t => t.Id).ToList();
                                await reservationService.AssignTablesToReservationAsync(res.Id, tableIdsToAssign);
                                updatedTableIds.AddRange(tableIdsToAssign);
                            }
                            else
                            {
                                if (!_warnedReservationIds.Contains(res.Id))
                                {
                                    await _hubContext.Clients.All.SendAsync("TableConflictWarning", new
                                    {
                                        ReservationId = res.Id,
                                        BranchId = res.BranchId,
                                        ReservationTime = res.ReservationTime,
                                        Message = $"Lưu ý đơn đặt bàn lúc {res.ReservationTime:HH:mm} (cần {res.TableCount} bàn) không đủ bàn trống trong 1 khu vực. Hệ thống không thể tự động xếp bàn, vui lòng tự xếp tay!"
                                    });
                                    _warnedReservationIds.Add(res.Id);
                                }
                            }
                        }
                    }

                    // 3. Lock Bàn (Bình thường)
                    var toLockList = await db.Reservations
                        .Include(r => r.Order).ThenInclude(o => o!.Table)
                        .Where(r => r.Status == "Pending" && r.Order != null && r.Order.Table != null 
                                 && (r.Order.Table.Status == "Empty" || r.Order.Table.Status == "Available")
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
                            if(child.Table != null && (child.Table.Status == "Empty" || child.Table.Status == "Available"))
                            {
                                child.Table.Status = "Reserved";
                                db.Tables.Update(child.Table);
                                updatedTableIds.Add(child.Table.Id);
                            }
                        }
                    }

                    // 4. Auto-Transfer (Cứu hộ bàn bị chiếm dụng đối với TẤT CẢ các bàn của đơn)
                    var pendingReservationsInThreshold = await db.Reservations
                        .Include(r => r.Order).ThenInclude(o => o!.Table)
                        .Where(r => r.Status == "Pending" && r.Order != null && r.Order.Table != null 
                                 && r.ReservationTime <= lockThreshold && r.ReservationTime > expiredTime)
                        .ToListAsync(stoppingToken);

                    foreach (var warnRes in pendingReservationsInThreshold)
                    {
                        var fatherOrder = warnRes.Order!;
                        var allRelatedOrders = new List<MenuGoBE.Models.Order> { fatherOrder };
                        
                        var childOrders = await db.Orders.Include(o => o.Table)
                            .Where(o => o.FatherId == fatherOrder.Id).ToListAsync(stoppingToken);
                        allRelatedOrders.AddRange(childOrders);

                        foreach (var orderToRescue in allRelatedOrders)
                        {
                            var table = orderToRescue.Table;
                            if (table != null && (table.Status == "Occupied" || table.Status == "Serving"))
                            {
                                // Cố gắng cứu hộ bằng cách tìm bàn trống khác trong cùng khu vực
                                var emptyTable = await db.Tables
                                    .Where(t => t.AreaId == table.AreaId && (t.Status == "Empty" || t.Status == "Available"))
                                    .FirstOrDefaultAsync(stoppingToken);

                                if (emptyTable != null)
                                {
                                    orderToRescue.TableId = emptyTable.Id;
                                    db.Orders.Update(orderToRescue);

                                    emptyTable.Status = "Reserved";
                                    db.Tables.Update(emptyTable);

                                    updatedTableIds.Add(table.Id); // Bàn cũ có thể cần update UI
                                    updatedTableIds.Add(emptyTable.Id);

                                    var area = await db.Areas.FirstOrDefaultAsync(a => a.Id == table.AreaId, stoppingToken);
                                    long branchId = area?.BranchId ?? 0;

                                    await _hubContext.Clients.All.SendAsync("TableConflictWarning", new
                                    {
                                        ReservationId = warnRes.Id,
                                        TableId = emptyTable.Id,
                                        TableName = emptyTable.Name,
                                        BranchId = branchId,
                                        ReservationTime = warnRes.ReservationTime,
                                        Message = $"Hệ thống tự động dời bàn: Bàn {table.Name} đã có khách. Một phần của đơn đặt bàn lúc {warnRes.ReservationTime:HH:mm} được chuyển sang bàn {emptyTable.Name}."
                                    });
                                }
                                else
                                {
                                    // Không tìm thấy bàn trống thay thế
                                    if (!_warnedReservationIds.Contains(warnRes.Id))
                                    {
                                        var area = await db.Areas.FirstOrDefaultAsync(a => a.Id == table.AreaId, stoppingToken);
                                        long branchId = area?.BranchId ?? 0;

                                        await _hubContext.Clients.All.SendAsync("TableConflictWarning", new
                                        {
                                            ReservationId = warnRes.Id,
                                            TableId = table.Id,
                                            TableName = table.Name,
                                            BranchId = branchId,
                                            ReservationTime = warnRes.ReservationTime,
                                            Message = $"KHẨN CẤP: Bàn {table.Name} (thuộc đơn {warnRes.TableCount} bàn) đã được đặt lúc {warnRes.ReservationTime:HH:mm} nhưng hiện vẫn đang có khách ngồi và KHÔNG còn bàn trống để đổi! Vui lòng sắp xếp ngay."
                                        });
                                        _warnedReservationIds.Add(warnRes.Id);
                                    }
                                }
                            }
                        }
                    }

                    // 5. Process late warnings: 15 mins late
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

                    if (expiredList.Any() || toLockList.Any() || unassignedList.Any() || pendingReservationsInThreshold.Any())
                    {
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }

                // Notify UI to refresh table statuses if any changes were made
                if (updatedTableIds.Any())
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveTableListUpdate", stoppingToken);
                }

                // Fixed 5-minute interval check
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReservationMonitorService Error: {ex.Message}");
            }
        }
    }
}
