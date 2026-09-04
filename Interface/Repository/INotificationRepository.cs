using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Notification;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface INotificationRepository
    {
        Task<List<Notification>> GetPagedNotificationsAsync(long[] branchIds, NotificationQueryDto query, string? userRole = null, long accountId = 0);
        Task<int> GetTotalCountAsync(long[] branchIds, NotificationQueryDto query, string? userRole = null, long accountId = 0);
        Task<List<Notification>> GetRecentNotificationsAsync(long[] branchIds, int limit, string? userRole = null, long accountId = 0);
        Task<int> GetUnreadCountAsync(long[] branchIds, string? userRole = null, long accountId = 0);
        Task<Notification?> GetByIdAsync(long id);
        Task CreateAsync(Notification notification);
        Task UpdateAsync(Notification notification);
        Task MarkAllAsReadAsync(long[] branchIds, string? userRole = null, long accountId = 0);
        Task SaveChangesAsync();
    }
}
