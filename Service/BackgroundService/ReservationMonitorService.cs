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
using MenuGoBE.Interface.Services;
using MenuGoBE.Dtos.Reservation;

namespace MenuGoBE.Service;

public class ReservationMonitorService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IReservationNotificationQueue _notificationQueue;
    private readonly HashSet<long> _warnedReservationIds = new HashSet<long>();
    private readonly HashSet<long> _lateWarnedReservationIds = new HashSet<long>();
    private readonly HashSet<long> _reminderSentIds = new HashSet<long>();
    private readonly HashSet<long> _expiredEmailSentIds = new HashSet<long>();

    public ReservationMonitorService(
        IServiceProvider serviceProvider, 
        IHubContext<NotificationHub> hubContext,
        IReservationNotificationQueue notificationQueue)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _notificationQueue = notificationQueue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(25), stoppingToken);
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
                        .Include(r => r.Branch)
                        .Include(r => r.Order).ThenInclude(o => o!.Table)
                        .Where(r => (r.Status == "Pending" || r.Status == "Confirmed") && r.Order != null && r.Order.Table != null && r.ReservationTime <= expiredTime)
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

                    // Email: Thông báo khách bàn đã bị tự động hủy (30 phút trễ)
                    foreach (var expired in expiredList)
                    {
                        if (!_expiredEmailSentIds.Contains(expired.Id))
                        {
                            var customer = await db.Customers.FindAsync(expired.CustomerId);
                            if (customer != null)
                            {
                                _ = _notificationQueue.QueueAsync(new ReservationEmailNotification
                                {
                                    CustomerId = expired.CustomerId,
                                    CustomerEmail = customer.Email,
                                    CustomerName = customer.Name,
                                    EventType = "AutoCancelled30Min",
                                    ReservationTime = expired.ReservationTime,
                                    BranchName = expired.Branch?.Name ?? "Chi nhánh",
                                    ReservationId = expired.Id,
                                    TableNames = expired.Order?.Table?.Name
                                });
                            }
                            _expiredEmailSentIds.Add(expired.Id);
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
                        .Include(r => r.Branch)
                        .Include(r => r.Order).ThenInclude(o => o!.Table)
                        .Where(r => (r.Status == "Pending" || r.Status == "Confirmed") && r.Order != null && r.Order.Table != null 
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

                    // Email: Nhắc lịch cho khách (30 phút trước giờ đặt bàn)
                    foreach (var res in toLockList)
                    {
                        if (!_reminderSentIds.Contains(res.Id))
                        {
                            var customer = await db.Customers.FindAsync(res.CustomerId);
                            if (customer != null)
                            {
                                _ = _notificationQueue.QueueAsync(new ReservationEmailNotification
                                {
                                    CustomerId = res.CustomerId,
                                    CustomerEmail = customer.Email,
                                    CustomerName = customer.Name,
                                    EventType = "Reminder30Min",
                                    ReservationTime = res.ReservationTime,
                                    BranchName = res.Branch?.Name ?? "Chi nhánh",
                                    ReservationId = res.Id,
                                    TableNames = res.Order?.Table?.Name
                                });
                            }
                            _reminderSentIds.Add(res.Id);
                        }
                    }

                    // 4. Auto-Transfer (Cứu hộ bàn bị chiếm dụng đối với TẤT CẢ các bàn của đơn)
                    var pendingReservationsInThreshold = await db.Reservations
                        .Include(r => r.Branch)
                        .Include(r => r.Order).ThenInclude(o => o!.Table)
                        .Where(r => (r.Status == "Pending" || r.Status == "Confirmed") && r.Order != null && r.Order.Table != null 
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
                                // Nếu đơn đặt nhiều bàn (bàn ghép), không tự động chuyển từng bàn lẻ tẻ vì sẽ làm mất vị trí liền kề.
                                // -> Chuyển thẳng xuống báo động đỏ yêu cầu xếp tay.
                                bool canAutoTransfer = allRelatedOrders.Count == 1;
                                MenuGoBE.Models.Table? emptyTable = null;

                                if (canAutoTransfer)
                                {
                                    // Cố gắng cứu hộ bằng cách tìm bàn trống khác trong cùng khu vực
                                    emptyTable = await db.Tables
                                        .Where(t => t.AreaId == table.AreaId && (t.Status == "Empty" || t.Status == "Available"))
                                        .FirstOrDefaultAsync(stoppingToken);
                                }

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

                                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                                    {
                                        Type = "auto-transfer",
                                        Title = "Hệ Thống Tự Xếp Bàn",
                                        Message = $"{table.Name} đã có khách, hệ thống tự động dời đơn đặt bàn lúc {warnRes.ReservationTime:HH:mm} sang bàn {emptyTable.Name}.",
                                        BranchId = branchId,
                                        CreatedAt = DateTime.UtcNow
                                    });
                                }
                                else
                                {
                                    // Không tìm thấy bàn trống thay thế HOẶC đơn nhiều bàn
                                    if (!_warnedReservationIds.Contains(warnRes.Id))
                                    {
                                        var area = await db.Areas.FirstOrDefaultAsync(a => a.Id == table.AreaId, stoppingToken);
                                        long branchId = area?.BranchId ?? 0;

                                        string warningReason = canAutoTransfer ? "KHÔNG còn bàn trống tương đương để đổi!" : "hệ thống KHÔNG hỗ trợ tự dời bàn lẻ tẻ cho đơn ghép nhiều bàn!";
                                        
                                        await _hubContext.Clients.All.SendAsync("TableConflictWarning", new
                                        {
                                            ReservationId = warnRes.Id,
                                            TableId = table.Id,
                                            TableName = table.Name,
                                            BranchId = branchId,
                                            ReservationTime = warnRes.ReservationTime,
                                            Message = $"KHẨN CẤP: {table.Name} (thuộc đơn {warnRes.TableCount} bàn) đã được đặt lúc {warnRes.ReservationTime:HH:mm} nhưng hiện vẫn đang có khách ngồi và {warningReason} Vui lòng tự sắp xếp."
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
                        .Include(r => r.Branch)
                        .Include(r => r.Order).ThenInclude(o => o!.Table)
                        .Where(r => (r.Status == "Pending" || r.Status == "Confirmed") && r.ReservationTime <= lateWarningThreshold && r.ReservationTime > expiredTime)
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

                        // Email: Nhắc nhở khách đã trễ 15 phút
                        var lateCustomer = await db.Customers.FindAsync(lateWarn.CustomerId);
                        if (lateCustomer != null)
                        {
                            _ = _notificationQueue.QueueAsync(new ReservationEmailNotification
                            {
                                CustomerId = lateWarn.CustomerId,
                                CustomerEmail = lateCustomer.Email,
                                CustomerName = lateCustomer.Name,
                                EventType = "LateWarning15Min",
                                ReservationTime = lateWarn.ReservationTime,
                                BranchName = lateWarn.Branch?.Name ?? "Chi nhánh",
                                ReservationId = lateWarn.Id,
                                TableNames = tableName
                            });
                        }
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
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReservationMonitorService Error: {ex.Message}");
            }

            // Luôn delay 5 phút trước lần quét kế tiếp kể cả khi có lỗi
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
