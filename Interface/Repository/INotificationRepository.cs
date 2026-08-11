using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Notification;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface INotificationRepository
    {
        Task<List<Notification>> GetPagedNotificationsAsync(long[] branchIds, NotificationQueryDto query);
        Task<int> GetTotalCountAsync(long[] branchIds, NotificationQueryDto query);
        Task<List<Notification>> GetRecentNotificationsAsync(long[] branchIds, int limit);
        Task<int> GetUnreadCountAsync(long[] branchIds);
        Task<Notification?> GetByIdAsync(long id);
        Task CreateAsync(Notification notification);
        Task UpdateAsync(Notification notification);
        Task SaveChangesAsync();
    }
}
