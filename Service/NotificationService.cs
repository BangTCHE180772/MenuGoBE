using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Base;
using MenuGoBE.Dtos.Notification;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly AppDbContext _context;

        public NotificationService(INotificationRepository repo, AppDbContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task<PagedResult<NotificationViewDto>> GetPagedNotificationsAsync(long[] branchIds, NotificationQueryDto query)
        {
            await SyncBranchAlertsAsync(branchIds);

            var notifications = await _repo.GetPagedNotificationsAsync(branchIds, query);
            var totalCount = await _repo.GetTotalCountAsync(branchIds, query);

            var items = notifications.Select(MapToViewDto).ToList();

            return new PagedResult<NotificationViewDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<List<NotificationViewDto>> GetRecentNotificationsAsync(long[] branchIds)
        {
            await SyncBranchAlertsAsync(branchIds);

            var notifications = await _repo.GetRecentNotificationsAsync(branchIds, 5);
            return notifications.Select(MapToViewDto).ToList();
        }

        public async Task<int> GetUnreadCountAsync(long[] branchIds)
        {
            return await _repo.GetUnreadCountAsync(branchIds);
        }

        public async Task<bool> MarkAsReadAsync(long id, long[] branchIds)
        {
            var notification = await _repo.GetByIdAsync(id);
            if (notification == null || !branchIds.Contains(notification.BranchId))
            {
                return false;
            }

            notification.IsRead = true;
            await _repo.UpdateAsync(notification);
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task SyncBranchAlertsAsync(long[] branchIds)
        {
            var todayUtc = DateTime.UtcNow.Date;
            var todayLocal = DateTime.UtcNow.AddHours(7).Date;
            var startUtc = DateTime.SpecifyKind(todayLocal.AddHours(-7), DateTimeKind.Utc);
            var endUtc = DateTime.SpecifyKind(todayLocal.AddDays(1).AddHours(-7).AddTicks(-1), DateTimeKind.Utc);

            var newNotifications = new List<Notification>();

            foreach (var branchId in branchIds)
            {
                var lowStockBInventories = await _context.BInventories
                    .Include(bi => bi.Product)
                    .Where(bi => bi.BranchId == branchId && bi.IsManageQuantity && bi.Quantity <= bi.MinStorage)
                    .ToListAsync();

                foreach (var bi in lowStockBInventories)
                {
                    var msg = $"Nguyên liệu \"{bi.Product.Name}\" hiện tại chỉ còn {bi.Quantity:N1}, dưới mức an toàn tối thiểu ({bi.MinStorage:N1}).";
                    var exists = await _context.Notifications.AnyAsync(n =>
                        n.BranchId == branchId &&
                        n.Type == "Inventory" &&
                        n.CreatedAt >= startUtc &&
                        n.CreatedAt <= endUtc &&
                        n.ReferenceType == "BInventory" &&
                        n.ReferenceId == bi.Id);

                    if (!exists)
                    {
                        newNotifications.Add(new Notification
                        {
                            BranchId = branchId,
                            Type = "Inventory",
                            Title = "Nguyên liệu sắp hết tồn kho",
                            Message = msg,
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false,
                            IsImportant = false,
                            Priority = "Warning",
                            ReferenceType = "BInventory",
                            ReferenceId = bi.Id,
                            RedirectUrl = "/inventory-management?tab=Product"
                        });
                    }
                }

                var todayExpenses = await _context.CashFlows
                    .Where(cf => !cf.IsDeleted && cf.BranchId == branchId && (int)cf.Direction == 2 && cf.BusinessDate >= startUtc && cf.BusinessDate <= endUtc)
                    .SumAsync(cf => cf.TotalAmount);

                if (todayExpenses > 50000000)
                {
                    var msg = $"Tổng chi phí hoạt động phát sinh hôm nay tại chi nhánh đạt mức: {todayExpenses:N0}đ, vượt quá ngưỡng cho phép.";
                    var exists = await _context.Notifications.AnyAsync(n =>
                        n.BranchId == branchId &&
                        n.Type == "Operational" &&
                        n.CreatedAt >= startUtc &&
                        n.CreatedAt <= endUtc &&
                        n.Title == "Chi phí tăng cao đột biến");

                    if (!exists)
                    {
                        newNotifications.Add(new Notification
                        {
                            BranchId = branchId,
                            Type = "Operational",
                            Title = "Chi phí tăng cao đột biến",
                            Message = msg,
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false,
                            IsImportant = true,
                            Priority = "Critical",
                            RedirectUrl = "/cashflow"
                        });
                    }
                }

                var todayDateOnly = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
                var noCheckInSchedules = await _context.WorkSchedules
                    .Include(ws => ws.Account)
                    .Where(ws => ws.BranchId == branchId && ws.WorkDate == todayDateOnly && ws.CheckInAt == null)
                    .ToListAsync();

                foreach (var ws in noCheckInSchedules)
                {
                    var msg = $"Nhân viên \"{ws.Account.Name}\" chưa thực hiện chấm công Check-in ca hôm nay.";
                    var exists = await _context.Notifications.AnyAsync(n =>
                        n.BranchId == branchId &&
                        n.Type == "Operational" &&
                        n.CreatedAt >= startUtc &&
                        n.CreatedAt <= endUtc &&
                        n.ReferenceType == "WorkSchedule" &&
                        n.ReferenceId == ws.Id);

                    if (!exists)
                    {
                        newNotifications.Add(new Notification
                        {
                            BranchId = branchId,
                            Type = "Operational",
                            Title = "Nhân viên chưa chấm công",
                            Message = msg,
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false,
                            IsImportant = false,
                            Priority = "Information",
                            ReferenceType = "WorkSchedule",
                            ReferenceId = ws.Id,
                            RedirectUrl = "/work-schedule"
                        });
                    }
                }

                var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
                var negativeDiscrepancies = await _context.DocumentDetails
                    .Include(dd => dd.Document)
                    .Include(dd => dd.BInventory)
                        .ThenInclude(bi => bi.Product)

                    .Where(dd => dd.BInventory.BranchId == branchId &&
                                dd.Document.Type == DocumentType.Check &&
                                dd.Document.PostedAt != null &&
                                dd.Document.PostedAt >= sevenDaysAgo &&
                                dd.ActualQuantity.HasValue &&
                                dd.SystemQuantity.HasValue &&
                                dd.ActualQuantity.Value < dd.SystemQuantity.Value)
                    .ToListAsync();

                foreach (var dd in negativeDiscrepancies)
                {
                    var loss = (dd.SystemQuantity ?? 0m) - (dd.ActualQuantity ?? 0m);
                    var msg = $"Nguyên liệu \"{dd.BInventory.Product.Name}\" bị hao hụt {loss:N1} trong đợt kiểm kho chốt ngày {dd.Document.PostedAt?.AddHours(7):dd/MM/yyyy}.";

                    var exists = await _context.Notifications.AnyAsync(n =>
                        n.BranchId == branchId &&
                        n.Type == "Quality" &&
                        n.ReferenceType == "DocumentDetail" &&
                        n.ReferenceId == dd.Id);

                    if (!exists)
                    {
                        newNotifications.Add(new Notification
                        {
                            BranchId = branchId,
                            Type = "Quality",
                            Title = "Sai số hao hụt kiểm kho",
                            Message = msg,
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false,
                            IsImportant = true,
                            Priority = "Warning",
                            ReferenceType = "DocumentDetail",
                            ReferenceId = dd.Id,
                            RedirectUrl = "/inventory-management?tab=Check"
                        });
                    }
                }

                var qualityCheckInventories = await _context.BInventories
                    .Include(bi => bi.Product)
                    .Where(bi => bi.BranchId == branchId && bi.IsManageQuantity)
                    .OrderBy(bi => bi.CreatedAt)
                    .Take(3)
                    .ToListAsync();

                foreach (var bi in qualityCheckInventories)
                {
                    var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                    var exists = await _context.Notifications.AnyAsync(n =>
                        n.BranchId == branchId &&
                        n.Type == "ShelfLife" &&
                        n.ReferenceType == "BInventoryCheck" &&
                        n.ReferenceId == bi.Id &&
                        n.CreatedAt >= thirtyDaysAgo);

                    if (!exists)
                    {
                        newNotifications.Add(new Notification
                        {
                            BranchId = branchId,
                            Type = "ShelfLife",
                            Title = "Nguyên liệu cần kiểm tra chất lượng",
                            Message = $"Nguyên liệu \"{bi.Product.Name}\" chưa được kiểm tra chất lượng bảo quản và hạn sử dụng định kỳ trong 30 ngày qua.",
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false,
                            IsImportant = false,
                            Priority = "Information",
                            ReferenceType = "BInventoryCheck",
                            ReferenceId = bi.Id,
                            RedirectUrl = "/inventory-management?tab=Product"
                        });
                    }
                }
            }

            if (newNotifications.Any())
            {
                foreach (var notif in newNotifications)
                {
                    await _repo.CreateAsync(notif);
                }
                await _repo.SaveChangesAsync();
            }
        }

        private NotificationViewDto MapToViewDto(Notification notif)
        {
            return new NotificationViewDto
            {
                Id = notif.Id,
                BranchId = notif.BranchId,
                BranchName = notif.Branch?.Name ?? $"Branch #{notif.BranchId}",
                Type = notif.Type,
                Title = notif.Title,
                Message = notif.Message,
                CreatedAt = notif.CreatedAt,
                IsRead = notif.IsRead,
                IsImportant = notif.IsImportant,
                Priority = notif.Priority,
                ReferenceType = notif.ReferenceType,
                ReferenceId = notif.ReferenceId,
                RedirectUrl = notif.RedirectUrl
            };
        }
    }
}
