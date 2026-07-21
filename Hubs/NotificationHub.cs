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
    }
}
