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

        #region Lấy danh sách thông báo phân trang
        /// <summary>
        /// Đồng bộ cảnh báo vận hành và lấy danh sách thông báo có phân trang.
        /// </summary>
        public async Task<PagedResult<NotificationViewDto>> GetPagedNotificationsAsync(long[] branchIds, NotificationQueryDto query, string? userRole = null, long accountId = 0)
        {
            // await SyncBranchAlertsAsync(branchIds); // Moved to NotificationSyncWorker

            var notifications = await _repo.GetPagedNotificationsAsync(branchIds, query, userRole, accountId);
            var totalCount = await _repo.GetTotalCountAsync(branchIds, query, userRole, accountId);

            var items = notifications.Select(MapToViewDto).ToList();

            return new PagedResult<NotificationViewDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
        #endregion

        #region Lấy danh sách thông báo gần đây (cho Chuông)
        /// <summary>
        /// Đồng bộ cảnh báo và lấy 15 thông báo gần nhất cho chuông thông báo.
        /// </summary>
        public async Task<List<NotificationViewDto>> GetRecentNotificationsAsync(long[] branchIds, string? userRole = null, long accountId = 0)
        {
            // await SyncBranchAlertsAsync(branchIds); // Moved to NotificationSyncWorker

            var notifications = await _repo.GetRecentNotificationsAsync(branchIds, 15, userRole, accountId);
            return notifications.Select(MapToViewDto).ToList();
        }
        #endregion

        #region Đếm số thông báo chưa đọc
        /// <summary>
        /// Đếm số lượng thông báo chưa đọc.
        /// </summary>
        public async Task<int> GetUnreadCountAsync(long[] branchIds, string? userRole = null, long accountId = 0)
        {
            return await _repo.GetUnreadCountAsync(branchIds, userRole, accountId);
        }
        #endregion

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

        public async Task<bool> MarkAllAsReadAsync(long[] branchIds, string? userRole = null, long accountId = 0)
        {
            await _repo.MarkAllAsReadAsync(branchIds, userRole, accountId);
            return true;
        }

        public async Task SyncBranchAlertsAsync(long[] branchIds)
        {
            var todayUtc = DateTime.UtcNow.Date;
            var todayLocal = DateTime.UtcNow.AddHours(7).Date;
            var startUtc = DateTime.SpecifyKind(todayLocal.AddHours(-7), DateTimeKind.Utc);
            var endUtc = DateTime.SpecifyKind(todayLocal.AddDays(1).AddHours(-7).AddTicks(-1), DateTimeKind.Utc);
            var todayDateOnly = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var nowLocalTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

            var newNotifications = new List<Notification>();

            foreach (var branchId in branchIds)
            {
                #region Dọn dẹp thông báo chấm công không còn hiệu lực
                // Lấy danh sách ca làm chưa check-in thực tế trong hôm nay
                var activeUncheckedSchedules = await _context.WorkSchedules
                    .Include(ws => ws.Account)
                    .Where(ws => ws.BranchId == branchId && ws.WorkDate == todayDateOnly && ws.CheckInAt == null)
                    .ToListAsync();

                var activeUncheckedScheduleIds = activeUncheckedSchedules.Select(ws => ws.Id).ToHashSet();
                var activeUncheckedAccountIds = activeUncheckedSchedules.Select(ws => ws.AccountId).ToHashSet();
                var activeUncheckedAccountNames = activeUncheckedSchedules
                    .Where(ws => ws.Account != null && !string.IsNullOrWhiteSpace(ws.Account.Name))
                    .Select(ws => ws.Account.Name.Trim().ToLower())
                    .ToHashSet();

                // Lấy tất cả thông báo thuộc loại Chấm công / Ca làm việc trong CSDL của chi nhánh
                var attendanceNotifs = await _context.Notifications
                    .Where(n => n.BranchId == branchId &&
                                (n.Type == "Attendance" ||
                                 n.Title.Contains("chấm công") ||
                                 n.Message.Contains("chấm công") ||
                                 n.Message.Contains("Check-in")))
                    .ToListAsync();

                var toRemoveAttendance = new List<Notification>();
                foreach (var notif in attendanceNotifs)
                {
                    bool isValid = false;

                    // 1. Kiểm tra theo ReferenceId ca làm việc
                    if (notif.ReferenceId.HasValue && activeUncheckedScheduleIds.Contains(notif.ReferenceId.Value))
                    {
                        isValid = true;
                    }
                    // 2. Kiểm tra theo AccountId nhân viên
                    else if (notif.AccountId.HasValue && activeUncheckedAccountIds.Contains(notif.AccountId.Value))
                    {
                        isValid = true;
                    }
                    // 3. Kiểm tra theo Tên nhân viên xuất hiện trong nội dung thông báo
                    else if (!string.IsNullOrWhiteSpace(notif.Message))
                    {
                        foreach (var name in activeUncheckedAccountNames)
                        {
                            if (notif.Message.ToLower().Contains(name))
                            {
                                isValid = true;
                                break;
                            }
                        }
                    }

                    if (!isValid)
                    {
                        toRemoveAttendance.Add(notif);
                    }
                }

                if (toRemoveAttendance.Any())
                {
                    _context.Notifications.RemoveRange(toRemoveAttendance);
                    await _context.SaveChangesAsync();
                }
                #endregion

                #region Cảnh báo Tồn kho mặt hàng theo ngưỡng thiết lập (Filter: Tồn kho - Hàng hết)
                var branchForStock = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId);
                decimal defaultStockCrit = branchForStock != null && branchForStock.StockCriticalThreshold > 0 ? branchForStock.StockCriticalThreshold : 20m;
                decimal defaultStockWarn = branchForStock != null && branchForStock.StockWarningThreshold > 0 ? branchForStock.StockWarningThreshold : 40m;

                var managedBInventories = await _context.BInventories
                    .Include(bi => bi.Product)
                        .ThenInclude(p => p.UnitConversions)
                            .ThenInclude(uc => uc.Unit)
                    .Where(bi => bi.BranchId == branchId && bi.IsManageQuantity && bi.IsAlertEnabled)
                    .ToListAsync();

                foreach (var bi in managedBInventories)
                {
                    decimal crit = bi.CustomCriticalThreshold ?? defaultStockCrit;
                    decimal warn = bi.CustomWarningThreshold ?? defaultStockWarn;
                    if (crit <= 0) crit = 20m;
                    if (warn <= 0) warn = 40m;

                    bool shouldAlert = false;
                    string title = "";
                    string priority = "Warning";
                    bool isImportant = false;
                    string msg = "";

                    var productName = bi.Product?.Name ?? "Nguyên liệu";

                    var unitName = bi.Product?.UnitConversions
                        ?.Where(uc => uc.BaseId == null)
                        ?.Select(uc => uc.Unit != null ? uc.Unit.Name : string.Empty)
                        ?.FirstOrDefault() ?? "Đơn vị";

                    if (bi.Quantity <= 0)
                    {
                        shouldAlert = true;
                        title = "Nguyên liệu hết tồn kho";
                        priority = "Critical";
                        isImportant = true;
                        msg = $"Mặt hàng \"{productName}\" đã hết tồn kho (SL tồn: {bi.Quantity:N1} {unitName}). Cần nhập hàng ngay!";
                    }
                    else if (bi.Quantity <= crit)
                    {
                        shouldAlert = true;
                        title = "Nguyên liệu tồn kho cấp bách";
                        priority = "Critical";
                        isImportant = true;
                        msg = $"Mặt hàng \"{productName}\" hiện còn {bi.Quantity:N1} {unitName}, dưới mức cấp bách ({crit:N1} {unitName}). Cần nhập bổ sung ngay!";
                    }
                    else if (bi.Quantity <= warn)
                    {
                        shouldAlert = true;
                        title = "Cảnh báo tồn kho sắp hết";
                        priority = "Warning";
                        isImportant = false;
                        msg = $"Mặt hàng \"{productName}\" hiện còn {bi.Quantity:N1} {unitName}, dưới mức cảnh báo ({warn:N1} {unitName}).";
                    }

                    if (shouldAlert)
                    {
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
                                Title = title,
                                Message = msg,
                                CreatedAt = DateTime.UtcNow,
                                IsRead = false,
                                IsImportant = isImportant,
                                Priority = priority,
                                ReferenceType = "BInventory",
                                ReferenceId = bi.Id,
                                RedirectUrl = $"/inventory-management?tab=Product&search={Uri.EscapeDataString(productName)}&productId={bi.ProductId}&inventoryId={bi.Id}&expand=true"
                            });
                        }
                    }
                }
                #endregion

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

                // 1. Cảnh báo chấm công (Chưa chấm công hoặc Đến giờ bắt đầu chấm công)
                var todaySchedules = await _context.WorkSchedules
                    .Include(ws => ws.Account)
                    .Include(ws => ws.Shift)
                    .Where(ws => ws.BranchId == branchId && ws.WorkDate == todayDateOnly)
                    .ToListAsync();

                foreach (var ws in todaySchedules)
                {
                    var shiftStart = ws.Shift != null ? ws.Shift.StartTime : new TimeOnly(8, 0);
                    var shiftEnd = ws.Shift != null ? ws.Shift.EndTime : new TimeOnly(17, 0);
                    var employeeName = ws.Account?.Name ?? "Nhân viên";

                    // A. Đã quá giờ vào ca (> 15 phút) nhưng chưa chấm công Check-in
                    if (ws.CheckInAt == null && nowLocalTime >= shiftStart.AddMinutes(15) && nowLocalTime <= shiftEnd)
                    {
                        var msg = $"Nhân viên \"{employeeName}\" đã quá giờ vào ca ({shiftStart:HH:mm} - {shiftEnd:HH:mm}) nhưng chưa thực hiện chấm công Check-in.";
                        var exists = await _context.Notifications.AnyAsync(n =>
                            n.BranchId == branchId &&
                            n.Type == "Attendance" &&
                            n.CreatedAt >= startUtc &&
                            n.CreatedAt <= endUtc &&
                            n.ReferenceType == "WorkScheduleLate" &&
                            n.ReferenceId == ws.Id);

                        if (!exists)
                        {
                            newNotifications.Add(new Notification
                            {
                                BranchId = branchId,
                                AccountId = ws.AccountId,
                                Type = "Attendance",
                                Title = "Nhân viên chưa chấm công vào ca",
                                Message = msg,
                                CreatedAt = DateTime.UtcNow,
                                IsRead = false,
                                IsImportant = true,
                                Priority = "Warning",
                                ReferenceType = "WorkScheduleLate",
                                ReferenceId = ws.Id,
                                RedirectUrl = $"/work-schedule?search={Uri.EscapeDataString(employeeName)}&accountId={ws.AccountId}&scheduleId={ws.Id}&date={ws.WorkDate:yyyy-MM-dd}"
                            });
                        }
                    }
                    // B. Ca làm việc sắp bắt đầu / Mở chấm công (trước giờ ca 30p đến 15p sau ca)
                    else if (ws.CheckInAt == null && nowLocalTime >= shiftStart.AddMinutes(-30) && nowLocalTime < shiftStart.AddMinutes(15))
                    {
                        var msg = $"Ca làm \"{ws.Shift?.Name ?? "Ca làm"}\" ({shiftStart:HH:mm} - {shiftEnd:HH:mm}) của \"{employeeName}\" đã mở cổng chấm công. Vui lòng chấm công trước khi vào ca.";
                        var exists = await _context.Notifications.AnyAsync(n =>
                            n.BranchId == branchId &&
                            n.Type == "Attendance" &&
                            n.CreatedAt >= startUtc &&
                            n.CreatedAt <= endUtc &&
                            n.ReferenceType == "WorkScheduleStart" &&
                            n.ReferenceId == ws.Id);

                        if (!exists)
                        {
                            newNotifications.Add(new Notification
                            {
                                BranchId = branchId,
                                AccountId = ws.AccountId,
                                Type = "Attendance",
                                Title = "Bắt đầu chấm công ca làm việc",
                                Message = msg,
                                CreatedAt = DateTime.UtcNow,
                                IsRead = false,
                                IsImportant = false,
                                Priority = "Information",
                                ReferenceType = "WorkScheduleStart",
                                ReferenceId = ws.Id,
                                RedirectUrl = $"/work-schedule?search={Uri.EscapeDataString(employeeName)}&accountId={ws.AccountId}&scheduleId={ws.Id}&date={ws.WorkDate:yyyy-MM-dd}"
                            });
                        }
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
                            RedirectUrl = $"/inventory-management?tab=Product&search={Uri.EscapeDataString(dd.BInventory.Product.Name)}&productId={dd.BInventory.ProductId}&inventoryId={dd.BInventory.Id}&expand=true"
                        });
                    }
                }

                #region 2. Cảnh báo Lô nguyên liệu theo hạn dùng và thiết lập ngưỡng (Filter: ⏳ Hạn sử dụng)
                var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId);
                int criticalDays = branch?.BatchCriticalDays >= 7 ? branch.BatchCriticalDays : 7;
                int warningDays = branch?.BatchWarningDays >= 14 ? branch.BatchWarningDays : 14;

                // Chỉ quét các lô Đang hoạt động, còn tồn thực tế và không bị tắt chuông thông báo
                var activeBatches = await _context.BInventoryBatches
                    .Include(b => b.BInventory)
                        .ThenInclude(bi => bi.Product)
                    .Where(b => b.BInventory.BranchId == branchId &&
                                b.Status == BatchStatus.Active &&
                                b.QuantityRemaining > 0 &&
                                !b.IsNotificationMuted)
                    .ToListAsync();

                foreach (var batch in activeBatches)
                {
                    bool shouldAlert = false;
                    string title = "";
                    string priority = "Warning";
                    bool isImportant = false;
                    string msg = "";

                    if (!batch.ExpiryDate.HasValue)
                    {
                        // Lô không có date: Tạo thông báo cấp bách để kiểm tra mỗi ngày
                        shouldAlert = true;
                        title = "Lô nguyên liệu chưa có hạn sử dụng";
                        priority = "Critical";
                        isImportant = true;
                        msg = $"Lô \"{batch.BatchCode}\" của nguyên liệu \"{batch.BInventory.Product.Name}\" (SL tồn: {batch.QuantityRemaining:N1}) chưa có ngày hết hạn. Vui lòng kiểm tra và cập nhật hạn sử dụng.";
                    }
                    else
                    {
                        var expDate = batch.ExpiryDate.Value.Date;
                        int daysRemaining = (int)(expDate - todayUtc).TotalDays;

                        if (daysRemaining <= 0)
                        {
                            // Hết hạn hoặc còn 0 ngày: Thông báo Cấp bách
                            shouldAlert = true;
                            title = "Lô nguyên liệu đã hết hạn sử dụng";
                            priority = "Critical";
                            isImportant = true;
                            msg = $"Lô \"{batch.BatchCode}\" của nguyên liệu \"{batch.BInventory.Product.Name}\" (SL tồn: {batch.QuantityRemaining:N1}) đã hết hạn vào ngày {batch.ExpiryDate.Value.AddHours(7):dd/MM/yyyy}. Cần xử lý xuất hủy ngay!";
                        }
                        else if (daysRemaining <= criticalDays)
                        {
                            // Cận date cấp bách: Thông báo Cấp bách
                            shouldAlert = true;
                            title = "Lô nguyên liệu cận date cấp bách";
                            priority = "Critical";
                            isImportant = true;
                            msg = $"Lô \"{batch.BatchCode}\" của nguyên liệu \"{batch.BInventory.Product.Name}\" (SL tồn: {batch.QuantityRemaining:N1}) chỉ còn {daysRemaining} ngày là hết hạn ({batch.ExpiryDate.Value.AddHours(7):dd/MM/yyyy}). Cần ưu tiên dùng trước!";
                        }
                        else if (daysRemaining <= warningDays)
                        {
                            // Sắp hết hạn theo ngưỡng cảnh báo: Thông báo Cảnh báo
                            shouldAlert = true;
                            title = "Lô nguyên liệu sắp hết hạn";
                            priority = "Warning";
                            isImportant = false;
                            msg = $"Lô \"{batch.BatchCode}\" của nguyên liệu \"{batch.BInventory.Product.Name}\" (SL tồn: {batch.QuantityRemaining:N1}) còn {daysRemaining} ngày nữa sẽ hết hạn ({batch.ExpiryDate.Value.AddHours(7):dd/MM/yyyy}).";
                        }
                    }

                    if (shouldAlert)
                    {
                        var exists = await _context.Notifications.AnyAsync(n =>
                            n.BranchId == branchId &&
                            n.Type == "ShelfLife" &&
                            n.CreatedAt >= startUtc &&
                            n.CreatedAt <= endUtc &&
                            n.ReferenceType == "BInventoryBatch" &&
                            n.ReferenceId == batch.Id);

                        if (!exists)
                        {
                            newNotifications.Add(new Notification
                            {
                                BranchId = branchId,
                                Type = "ShelfLife",
                                Title = title,
                                Message = msg,
                                CreatedAt = DateTime.UtcNow,
                                IsRead = false,
                                IsImportant = isImportant,
                                Priority = priority,
                                ReferenceType = "BInventoryBatch",
                                ReferenceId = batch.Id,
                                RedirectUrl = $"/inventory-management?tab=BatchExpiry&search={Uri.EscapeDataString(batch.BatchCode)}&batchId={batch.Id}"
                            });
                        }
                    }
                }
                #endregion

                #region Cảnh báo Hạn chốt bảng lương dành cho Quản lý
                if (branch != null)
                {
                    int payday = branch.PayrollPayday > 0 ? branch.PayrollPayday : 10;
                    int lockDeadlineDay = Math.Max(1, payday - 2);

                    int targetMonth = todayLocal.Day <= payday ? todayLocal.AddMonths(-1).Month : todayLocal.Month;
                    int targetYear = todayLocal.Day <= payday ? todayLocal.AddMonths(-1).Year : todayLocal.Year;

                    var activeBranchAccountIds = await _context.Contracts
                        .Where(c => c.BranchId == branchId && c.Status == "Active")
                        .Select(c => c.AccountId)
                        .Distinct()
                        .ToListAsync();

                    if (activeBranchAccountIds.Any())
                    {
                        var finalizedAccountIds = await _context.Payrolls
                            .Where(p => p.BranchId == branchId &&
                                        p.Month == targetMonth &&
                                        p.Year == targetYear &&
                                        (p.Status == PayrollStatus.Locked ||
                                         p.Status == PayrollStatus.Approved ||
                                         p.Status == PayrollStatus.Paid))
                            .Select(p => p.AccountId)
                            .ToListAsync();

                        int unfinalizedCount = activeBranchAccountIds.Except(finalizedAccountIds).Count();

                        if (unfinalizedCount > 0)
                        {
                            var title = "CẢNH BÁO HẠN CHỐT LƯƠNG DÀNH CHO QUẢN LÝ";
                            var msg = $"Chi nhánh {branch.Name} quy định trả lương vào ngày {payday} hàng tháng. Quản lý phải hoàn tất chốt bảng lương trước ngày {lockDeadlineDay}! Hiện đang có {unfinalizedCount} nhân sự chưa được chốt lương. Vui lòng rà soát và bấm \"Chốt lương\" gửi Chủ cửa hàng phê duyệt.";

                            var existingNotif = await _context.Notifications.FirstOrDefaultAsync(n =>
                                n.BranchId == branchId &&
                                n.Type == "Operational" &&
                                n.CreatedAt >= startUtc &&
                                n.CreatedAt <= endUtc &&
                                n.ReferenceType == "PayrollDeadline");

                            if (existingNotif == null)
                            {
                                newNotifications.Add(new Notification
                                {
                                    BranchId = branchId,
                                    Type = "Operational",
                                    Title = title,
                                    Message = msg,
                                    CreatedAt = DateTime.UtcNow,
                                    IsRead = false,
                                    IsImportant = true,
                                    Priority = todayLocal.Day >= lockDeadlineDay ? "Critical" : "Warning",
                                    ReferenceType = "PayrollDeadline",
                                    RedirectUrl = "/payroll"
                                });
                            }
                            else if (existingNotif.Message != msg)
                            {
                                existingNotif.Message = msg;
                                existingNotif.Priority = todayLocal.Day >= lockDeadlineDay ? "Critical" : "Warning";
                                _context.Notifications.Update(existingNotif);
                            }
                        }
                    }
                }
                #endregion
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
