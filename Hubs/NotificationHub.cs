using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MenuGoBE.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task JoinDeviceGroup(string deviceToken)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceToken}");
        }

        public async Task JoinBranchGroup(long branchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"branch_{branchId}");
        }

        public async Task JoinUserGroup(long accountId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{accountId}");
        }
    }
}
