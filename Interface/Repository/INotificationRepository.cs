using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Notification;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface INotificationRepository
    {
        Task<List<Notification>> GetPagedNotificationsAsync(long[] branchIds, NotificationQueryDto query, string? userRole = null);
        Task<int> GetTotalCountAsync(long[] branchIds, NotificationQueryDto query, string? userRole = null);
        Task<List<Notification>> GetRecentNotificationsAsync(long[] branchIds, int limit, string? userRole = null);
        Task<int> GetUnreadCountAsync(long[] branchIds, string? userRole = null);
        Task<Notification?> GetByIdAsync(long id);
        Task CreateAsync(Notification notification);
        Task UpdateAsync(Notification notification);
        Task SaveChangesAsync();
    }
}
