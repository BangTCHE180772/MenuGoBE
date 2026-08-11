using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Base;
using MenuGoBE.Dtos.Notification;

namespace MenuGoBE.Interface.Services
{
    public interface INotificationService
    {
        Task<PagedResult<NotificationViewDto>> GetPagedNotificationsAsync(long[] branchIds, NotificationQueryDto query);
        Task<List<NotificationViewDto>> GetRecentNotificationsAsync(long[] branchIds);
        Task<int> GetUnreadCountAsync(long[] branchIds);
        Task<bool> MarkAsReadAsync(long id, long[] branchIds);
        Task SyncBranchAlertsAsync(long[] branchIds);
    }
}
