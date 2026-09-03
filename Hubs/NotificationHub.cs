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
        /// KHÔNG unassign bàn. Waiter có thể bị disconnect tạm thời.
        /// Monitor service sẽ xử lý nếu không có LastActionAt quá lâu.
        /// Chỉ dọn dẹp connection map.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            lock (_lock)
            {
                _connectionMap.Remove(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }  
        
        public async Task JoinCustomerGroup(long customerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"customer_{customerId}");
        }

        public async Task JoinGuestGroup(string guestId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"guest_{guestId}");
        }

        public async Task LeaveGuestGroup(string guestId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"guest_{guestId}");
        }
    }
}
