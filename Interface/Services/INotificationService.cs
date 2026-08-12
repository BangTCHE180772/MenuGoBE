using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Base;
using MenuGoBE.Dtos.Notification;

namespace MenuGoBE.Interface.Services
{
    public interface INotificationService
    {
        Task<PagedResult<NotificationViewDto>> GetPagedNotificationsAsync(long[] branchIds, NotificationQueryDto query, string? userRole = null);
        Task<List<NotificationViewDto>> GetRecentNotificationsAsync(long[] branchIds, string? userRole = null);
        Task<int> GetUnreadCountAsync(long[] branchIds, string? userRole = null);
        Task<bool> MarkAsReadAsync(long id, long[] branchIds);
        Task SyncBranchAlertsAsync(long[] branchIds);
    }
}
