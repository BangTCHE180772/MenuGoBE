using Microsoft.AspNetCore.SignalR;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;

namespace MenuGoBE.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationHub> _logger;

        // Static dictionary: ConnectionId → AccountId (để biết ai disconnect)
        private static readonly Dictionary<string, long> _connectionMap = new();
        private static readonly object _lock = new();

        public NotificationHub(IServiceScopeFactory scopeFactory, ILogger<NotificationHub> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task JoinDeviceGroup(string deviceToken)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceToken}");
        }

        /// <summary>
        /// CHỈ Manager/Admin join group này. Họ LUÔN nhận mọi thông báo.
        /// </summary>
        public async Task JoinBranchGroup(long branchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"branch_{branchId}");
        }

        public async Task JoinUserGroup(long accountId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{accountId}");
        }

        /// <summary>
        /// Waiter join group cá nhân (để nhận targeted notification khi đã assign bàn).
        /// </summary>
        public async Task JoinWaiterGroup(long accountId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"waiter_{accountId}");
            lock (_lock) { _connectionMap[Context.ConnectionId] = accountId; }
        }

        /// <summary>
        /// Waiter/Cashier join group on-duty của branch.
        /// Waiter gọi khi vào WaiterMobileSessionPage.
        /// Cashier gọi khi vào CashierPage.
        /// </summary>
        public async Task JoinOnDuty(long branchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"on_duty_{branchId}");
        }

        public async Task LeaveOnDuty(long branchId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"on_duty_{branchId}");
        }

        public async Task LeaveWaiterGroup(long accountId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"waiter_{accountId}");
            lock (_lock) { _connectionMap.Remove(Context.ConnectionId); }
        }

        /// <summary>
        /// Khi nhân viên mất kết nối (tắt app, mất mạng, phone hết pin)
        /// → auto-unassign tất cả bàn của họ.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            long accountId = 0;
            lock (_lock)
            {
                if (_connectionMap.TryGetValue(Context.ConnectionId, out var id))
                {
                    accountId = id;
                    _connectionMap.Remove(Context.ConnectionId);
                }
            }

            if (accountId > 0)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var assignmentService = scope.ServiceProvider
                        .GetRequiredService<IOrderAssignmentService>();
                    var assignmentRepo = scope.ServiceProvider
                        .GetRequiredService<IOrderAssignmentRepository>();

                    var assignments = await assignmentRepo.GetByAccountIdAsync(accountId);

                    if (assignments.Any())
                    {
                        await assignmentService.ClearAllByAccountAsync(accountId);

                        foreach (var a in assignments)
                        {
                            var tableName = a.Order?.Table?.Name ?? $"Order #{a.OrderId}";
                            var payload = new
                            {
                                type = "assignment-orphaned",
                                tableId = a.Order?.TableId,
                                tableName,
                                orderId = a.OrderId,
                                message = $"📋 {tableName}: Nhân viên mất kết nối. Cần người nhận bàn!",
                                priority = "Warning",
                                timestamp = DateTime.UtcNow
                            };
                            await Clients.Group($"on_duty_{a.BranchId}").SendAsync("ReceiveNotification", payload);
                            await Clients.Group($"branch_{a.BranchId}").SendAsync("ReceiveNotification", payload);
                        }

                        _logger.LogInformation(
                            "Waiter disconnected: AccountId={AccountId}, unassigned {Count} orders",
                            accountId, assignments.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling disconnect for AccountId={AccountId}", accountId);
                }
            }

            await base.OnDisconnectedAsync(exception);
          
       }    
    public async Task JoinCustomerGroup(long customerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"customer_{customerId}");
        }
    }
}
