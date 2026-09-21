using MenuGoBE.Data;
using MenuGoBE.Hubs;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    /// <summary>
    /// Background service chạy mỗi 30 giây, thực hiện 3 nhiệm vụ:
    /// 1. Check đơn hàng chưa ai assign (unattended) → escalation
    /// 2. Check waiter đã assign nhưng không thao tác (stale) → reminder/escalation
    /// 3. Check waiter đã checkout/offline mà còn assignment (orphaned) → auto-unassign
    /// </summary>
    public class OrderAssignmentMonitorService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<OrderAssignmentMonitorService> _logger;

        public OrderAssignmentMonitorService(
            IServiceScopeFactory scopeFactory,
            IHubContext<NotificationHub> hubContext,
            ILogger<OrderAssignmentMonitorService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OrderAssignmentMonitorService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckUnattendedOrdersAsync();
                    await CheckStaleAssignmentsAsync();
                    await CheckOrphanedAssignmentsAsync();
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OrderAssignmentMonitor error");
                }
            }
        }

        /// <summary>
        /// Nhiệm vụ 1: Check đơn hàng CustomerPending chưa có ai assign.
        /// T+2min: Khẩn cấp cho on_duty + branch
        /// T+5min: Alert Manager (Critical)
        /// T+8min+: Lặp lại alert Manager mỗi 3 phút
        /// </summary>
        private async Task CheckUnattendedOrdersAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;

            var unattendedOrders = await db.Orders
                .Include(o => o.Table).ThenInclude(t => t.Area)
                .Include(o => o.Assignments)
                .Include(o => o.OrderDetails)
                .Where(o =>
                    o.Status == "Active" &&
                    !o.Assignments.Any(a => a.IsActive && a.AssignmentType == "Primary") &&
                    o.OrderDetails.Any(od =>
                        od.Status == "CustomerPending" &&
                        od.CreatedAt <= now.AddMinutes(-2)))
                .ToListAsync();

            foreach (var order in unattendedOrders)
            {
                var branchId = order.Table?.Area?.BranchId ?? 0;
                if (branchId == 0) continue;

                var oldestPending = order.OrderDetails
                    .Where(od => od.Status == "CustomerPending")
                    .Min(od => od.CreatedAt);
                var minutesWaiting = (now - oldestPending).TotalMinutes;

                if (minutesWaiting >= 8)
                {
                    var timeInCycle = (minutesWaiting - 8) % 3;
                    if (timeInCycle < 0.6)
                    {
                        await _hubContext.Clients.Group($"branch_{branchId}")
                            .SendAsync("ReceiveNotification", new
                            {
                                type = "unattended-manager-repeat",
                                tableId = order.TableId,
                                tableName = order.Table?.Name,
                                orderId = order.Id,
                                message = $"🚨 LẶP LẠI: {order.Table?.Name} KHÔNG CÓ AI PHỤC VỤ đã {(int)minutesWaiting} phút! Cần chỉ định nhân viên.",
                                priority = "Critical",
                                minutesWaiting = (int)minutesWaiting,
                                timestamp = now
                            });
                    }
                }
                else if (minutesWaiting >= 5)
                {
                    await _hubContext.Clients.Group($"branch_{branchId}")
                        .SendAsync("ReceiveNotification", new
                        {
                            type = "unattended-critical",
                            tableId = order.TableId,
                            tableName = order.Table?.Name,
                            orderId = order.Id,
                            message = $"⚠️ CẢNH BÁO: {order.Table?.Name} không có ai phục vụ đã {(int)minutesWaiting} phút!",
                            priority = "Critical",
                            timestamp = now
                        });
                }
                else
                {
                    var payload = new
                    {
                        type = "unattended-urgent",
                        tableId = order.TableId,
                        tableName = order.Table?.Name,
                        orderId = order.Id,
                        message = $"🔴 KHẨN CẤP: {order.Table?.Name} có khách gọi món chưa được xác nhận!",
                        priority = "Warning",
                        timestamp = now
                    };
                    await _hubContext.Clients.Group($"on_duty_{branchId}").SendAsync("ReceiveNotification", payload);
                    await _hubContext.Clients.Group($"branch_{branchId}").SendAsync("ReceiveNotification", payload);
                }
            }
        }

        /// <summary>
        /// Nhiệm vụ 2: Check waiter đã assign nhưng không thao tác (có món Ready chưa Served).
        /// > 3 phút: Reminder cho assigned waiter
        /// > 5 phút: Broadcast cho on-duty staff + Manager
        /// > 8 phút: Alert Manager (Critical)
        /// </summary>
        private async Task CheckStaleAssignmentsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IOrderAssignmentRepository>();
            var now = DateTime.UtcNow;

            var staleAssignments = await repo.GetStaleAssignmentsAsync(3);

            foreach (var assignment in staleAssignments)
            {
                var idleMinutes = (now - assignment.LastActionAt).TotalMinutes;
                var branchId = assignment.BranchId;
                var tableName = assignment.Order?.Table?.Name ?? $"Bàn #{assignment.Order?.TableId}";

                if (idleMinutes >= 8)
                {
                    await _hubContext.Clients.Group($"branch_{branchId}")
                        .SendAsync("ReceiveNotification", new
                        {
                            type = "stale-assignment-critical",
                            tableId = assignment.Order?.TableId,
                            tableName,
                            orderId = assignment.OrderId,
                            assignedAccountId = assignment.AccountId,
                            message = $"🚨 {tableName}: Nhân viên phụ trách không phản hồi đã {(int)idleMinutes} phút! Có món chờ bê ra.",
                            priority = "Critical",
                            timestamp = now
                        });
                }
                else if (idleMinutes >= 5)
                {
                    var payload = new
                    {
                        type = "stale-assignment-help",
                        tableId = assignment.Order?.TableId,
                        tableName,
                        orderId = assignment.OrderId,
                        message = $"📢 {tableName} cần hỗ trợ! Có món đã sẵn sàng nhưng chưa được bê ra.",
                        priority = "Warning",
                        timestamp = now
                    };
                    await _hubContext.Clients.Group($"on_duty_{branchId}").SendAsync("ReceiveNotification", payload);
                    await _hubContext.Clients.Group($"branch_{branchId}").SendAsync("ReceiveNotification", payload);
                }
                else
                {
                    await _hubContext.Clients.Group($"waiter_{assignment.AccountId}")
                        .SendAsync("ReceiveNotification", new
                        {
                            type = "stale-assignment-reminder",
                            tableId = assignment.Order?.TableId,
                            tableName,
                            orderId = assignment.OrderId,
                            message = $"⏰ Nhắc: {tableName} có món đã sẵn sàng, hãy bê ra cho khách!",
                            priority = "Information",
                            timestamp = now
                        });
                }
            }
        }

        /// <summary>
        /// Nhiệm vụ 3: Check assignments mà waiter đã checkout / không còn on-duty.
        /// → Auto-unassign + thông báo on-duty + branch cần người nhận bàn.
        /// </summary>
        private async Task CheckOrphanedAssignmentsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IOrderAssignmentRepository>();
            var service = scope.ServiceProvider.GetRequiredService<IOrderAssignmentService>();

            var orphaned = await repo.GetOrphanedAssignmentsAsync();

            foreach (var assignment in orphaned)
            {
                var branchId = assignment.BranchId;
                var tableId = assignment.Order?.TableId ?? 0;
                var tableName = assignment.Order?.Table?.Name ?? $"Bàn #{tableId}";

                await service.DeactivateAssignmentAsync(assignment.OrderId, "ShiftEnded");

                // Thử auto-assign lại cho người khác
                if (tableId > 0)
                {
                    await service.AutoAssignWaiterAsync(assignment.OrderId, tableId, branchId);
                }

                _logger.LogInformation(
                    "Orphaned assignment removed and re-assigned: OrderId={OrderId}, OldAccountId={AccountId} (no longer on duty)",
                    assignment.OrderId, assignment.AccountId);

                var payload = new
                {
                    type = "assignment-orphaned",
                    tableId = assignment.Order?.TableId,
                    tableName,
                    orderId = assignment.OrderId,
                    message = $"📋 {tableName}: Nhân viên phụ trách đã hết ca. Cần người nhận bàn!",
                    priority = "Warning",
                    timestamp = DateTime.UtcNow
                };
                await _hubContext.Clients.Group($"on_duty_{branchId}").SendAsync("ReceiveNotification", payload);
                await _hubContext.Clients.Group($"branch_{branchId}").SendAsync("ReceiveNotification", payload);
            }
        }
    }
}
