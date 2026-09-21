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

        public async Task<List<Notification>> GetPagedNotificationsAsync(long[] branchIds, NotificationQueryDto query, string? userRole = null, long accountId = 0)
        {
            var dbQuery = _context.Notifications
                .Include(n => n.Branch)
                .Where(n => branchIds.Contains(n.BranchId));

            dbQuery = ApplyFilters(dbQuery, query);
            dbQuery = ApplyRoleFilter(dbQuery, userRole, accountId);

            return await dbQuery
                .OrderByDescending(n => n.IsImportant)
                .ThenByDescending(n => n.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(long[] branchIds, NotificationQueryDto query, string? userRole = null, long accountId = 0)
        {
            var dbQuery = _context.Notifications.Where(n => branchIds.Contains(n.BranchId));
            dbQuery = ApplyFilters(dbQuery, query);
            dbQuery = ApplyRoleFilter(dbQuery, userRole, accountId);
            return await dbQuery.CountAsync();
        }

        public async Task<List<Notification>> GetRecentNotificationsAsync(long[] branchIds, int limit, string? userRole = null, long accountId = 0)
        {
            var todayLocal = DateTime.UtcNow.AddHours(7).Date;
            var startOfTodayUtc = DateTime.SpecifyKind(todayLocal.AddHours(-7), DateTimeKind.Utc);

            var dbQuery = _context.Notifications
                .Include(n => n.Branch)
                .Where(n => branchIds.Contains(n.BranchId) && !n.IsRead && n.CreatedAt >= startOfTodayUtc);

            dbQuery = ApplyRoleFilter(dbQuery, userRole, accountId);

            return await dbQuery
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(long[] branchIds, string? userRole = null, long accountId = 0)
        {
            var todayLocal = DateTime.UtcNow.AddHours(7).Date;
            var startOfTodayUtc = DateTime.SpecifyKind(todayLocal.AddHours(-7), DateTimeKind.Utc);

            var dbQuery = _context.Notifications.Where(n => branchIds.Contains(n.BranchId) && !n.IsRead && n.CreatedAt >= startOfTodayUtc);
            dbQuery = ApplyRoleFilter(dbQuery, userRole, accountId);

            return await dbQuery.CountAsync();
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

        public async Task MarkAllAsReadAsync(long[] branchIds, string? userRole = null, long accountId = 0)
        {
            var dbQuery = _context.Notifications.Where(n => !n.IsRead);
            if (branchIds != null && branchIds.Length > 0)
            {
                dbQuery = dbQuery.Where(n => branchIds.Contains(n.BranchId));
            }
            dbQuery = ApplyRoleFilter(dbQuery, userRole, accountId);

            var unreadList = await dbQuery.ToListAsync();
            if (unreadList.Any())
            {
                foreach (var n in unreadList)
                {
                    n.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }
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

            if (!string.IsNullOrEmpty(query.Type) && !query.Type.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var type = query.Type.Trim().ToLower();
                if (type == "attendance")
                {
                    dbQuery = dbQuery.Where(n => n.Type.ToLower() == "attendance" ||
                        (n.Type.ToLower() == "operational" && ((n.ReferenceType != null && n.ReferenceType.StartsWith("WorkSchedule")) || n.Title.ToLower().Contains("chấm công"))));
                }
                else if (type == "payroll")
                {
                    dbQuery = dbQuery.Where(n => n.Type.ToLower() == "payroll" ||
                        (n.Type.ToLower() == "operational" && (n.ReferenceType == "PayrollDeadline" || n.Title.ToLower().Contains("lương"))));
                }
                else if (type == "reservation")
                {
                    dbQuery = dbQuery.Where(n => n.Type.ToLower() == "reservation" ||
                        (n.ReferenceType != null && n.ReferenceType.ToLower() == "reservation") ||
                        n.Title.ToLower().Contains("đặt bàn"));
                }
                else if (type == "shelflife")
                {
                    dbQuery = dbQuery.Where(n => n.Type.ToLower() == "shelflife" ||
                        n.Title.ToLower().Contains("hết hạn") ||
                        n.Title.ToLower().Contains("hạn sử dụng"));
                }
                else if (type == "inventory")
                {
                    dbQuery = dbQuery.Where(n => n.Type.ToLower() == "inventory" ||
                        n.Title.ToLower().Contains("tồn kho") ||
                        n.Title.ToLower().Contains("hết hàng"));
                }
                else if (type == "quality")
                {
                    dbQuery = dbQuery.Where(n => n.Type.ToLower() == "quality" ||
                        n.Title.ToLower().Contains("kiểm kê") ||
                        n.Title.ToLower().Contains("hao hụt"));
                }
                else if (type == "order")
                {
                    dbQuery = dbQuery.Where(n => (n.Type.ToLower() == "order" || n.Title.ToLower().Contains("đơn hàng") || n.Title.ToLower().Contains("món ăn")) && !n.Title.ToLower().Contains("đặt bàn"));
                }
                else if (type == "chat")
                {
                    dbQuery = dbQuery.Where(n => n.Type.ToLower() == "chat" || n.Title.ToLower().Contains("tin nhắn"));
                }
                else
                {
                    dbQuery = dbQuery.Where(n => n.Type.ToLower() == type);
                }
            }

            if (!string.IsNullOrEmpty(query.Priority) && !query.Priority.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                dbQuery = dbQuery.Where(n => n.Priority == query.Priority);
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

        private IQueryable<Notification> ApplyRoleFilter(IQueryable<Notification> dbQuery, string? userRole, long accountId = 0)
        {
            if (string.IsNullOrEmpty(userRole))
            {
                if (accountId > 0)
                {
                    dbQuery = dbQuery.Where(n => n.AccountId == null || n.AccountId == accountId);
                }
                return dbQuery;
            }

            var role = userRole.ToLower();

            // Admin, Owner, Manager see all notifications for the branch
            bool isManagement = role == "admin" || role == "owner" || role == "manager" || role == "quản trị viên" || role == "chủ sở hữu" || role == "quản lý";
            if (isManagement)
            {
                return dbQuery;
            }

            // Chef, Kitchen see Inventory, Quality, ShelfLife, WorkSchedule, Attendance
            if (role == "chef" || role == "kitchen" || role == "bếp")
            {
                dbQuery = dbQuery.Where(n => n.Type == "Inventory" || n.Type == "Quality" || n.Type == "ShelfLife" || n.Type == "WorkSchedule" || n.Type == "Attendance");
            }
            // Cashier, Waiter see Operational, Attendance, Order, WorkSchedule
            else if (role == "cashier" || role == "waiter" || role == "thu ngân" || role == "phục vụ")
            {
                dbQuery = dbQuery.Where(n => n.Type == "Operational" || n.Type == "Attendance" || n.Type == "Order" || n.Type == "WorkSchedule");
            }

            // Staff (non-management):
            // For WorkSchedule or Attendance notifications (or ReferenceType starting with WorkSchedule),
            // staff MUST ONLY see notifications belonging to their own AccountId.
            // For non-schedule notifications, they can see general branch alerts (n.AccountId == null) or targeted alerts.
            if (accountId > 0)
            {
                dbQuery = dbQuery.Where(n =>
                    (n.Type == "WorkSchedule" || n.Type == "Attendance" || (n.ReferenceType != null && n.ReferenceType.StartsWith("WorkSchedule")))
                        ? n.AccountId == accountId
                        : (n.AccountId == null || n.AccountId == accountId));
            }

            return dbQuery;
        }
    }
}
