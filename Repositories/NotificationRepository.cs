using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Notification;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Notification>> GetPagedNotificationsAsync(long[] branchIds, NotificationQueryDto query)
        {
            var dbQuery = _context.Notifications
                .Include(n => n.Branch)
                .Where(n => branchIds.Contains(n.BranchId));

            dbQuery = ApplyFilters(dbQuery, query);

            return await dbQuery
                .OrderByDescending(n => n.IsImportant)
                .ThenByDescending(n => n.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(long[] branchIds, NotificationQueryDto query)
        {
            var dbQuery = _context.Notifications.Where(n => branchIds.Contains(n.BranchId));
            dbQuery = ApplyFilters(dbQuery, query);
            return await dbQuery.CountAsync();
        }

        public async Task<List<Notification>> GetRecentNotificationsAsync(long[] branchIds, int limit)
        {
            var dbQuery = _context.Notifications
                .Include(n => n.Branch)
                .Where(n => branchIds.Contains(n.BranchId));

            var list = await dbQuery.ToListAsync();

            return list
                .OrderBy(n => n.IsRead ? 1 : 0)
                .ThenByDescending(n => n.IsImportant)
                .ThenByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToList();
        }

        public async Task<int> GetUnreadCountAsync(long[] branchIds)
        {
            return await _context.Notifications
                .CountAsync(n => branchIds.Contains(n.BranchId) && !n.IsRead);
        }

        public async Task<Notification?> GetByIdAsync(long id)
        {
            return await _context.Notifications
                .Include(n => n.Branch)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task CreateAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
        }

        public async Task UpdateAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        private IQueryable<Notification> ApplyFilters(IQueryable<Notification> dbQuery, NotificationQueryDto query)
        {
            if (query.BranchId.HasValue && query.BranchId.Value > 0)
            {
                dbQuery = dbQuery.Where(n => n.BranchId == query.BranchId.Value);
            }

            if (query.FromDate.HasValue)
            {
                dbQuery = dbQuery.Where(n => n.CreatedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                dbQuery = dbQuery.Where(n => n.CreatedAt <= query.ToDate.Value);
            }

            if (!string.IsNullOrEmpty(query.Type))
            {
                dbQuery = dbQuery.Where(n => n.Type.Equals(query.Type, StringComparison.OrdinalIgnoreCase));
            }

            if (query.IsRead.HasValue)
            {
                dbQuery = dbQuery.Where(n => n.IsRead == query.IsRead.Value);
            }

            if (!string.IsNullOrEmpty(query.Search))
            {
                var search = query.Search.Trim().ToLower();
                dbQuery = dbQuery.Where(n => n.Title.ToLower().Contains(search) || n.Message.ToLower().Contains(search));
            }

            return dbQuery;
        }
    }
}
