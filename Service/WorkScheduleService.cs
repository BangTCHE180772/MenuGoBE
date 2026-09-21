using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.WorkSchedule;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MenuGoBE.Hubs;

namespace MenuGoBE.Service
{
    public class WorkScheduleService : IWorkScheduleService
    {
        private readonly IWorkScheduleRepository _repo;
        private readonly IMapper _mapper;
        private readonly IBranchRepository _branchRepo;
        private readonly IShiftRepository _shiftRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly AppDbContext _context;
        private readonly INotificationRepository _notificationRepo;
        private readonly IOrderAssignmentRepository _orderAssignmentRepo;

        public WorkScheduleService(
            IWorkScheduleRepository repo,
            IMapper mapper,
            IBranchRepository branchRepo,
            IShiftRepository shiftRepo,
            IAccountRepository accountRepo,
            IHubContext<NotificationHub> hubContext,
            AppDbContext context,
            INotificationRepository notificationRepo,
            IOrderAssignmentRepository orderAssignmentRepo)
        {
            _repo = repo;
            _mapper = mapper;
            _branchRepo = branchRepo;
            _shiftRepo = shiftRepo;
            _accountRepo = accountRepo;
            _hubContext = hubContext;
            _context = context;
            _notificationRepo = notificationRepo;
            _orderAssignmentRepo = orderAssignmentRepo;
        }



        private async Task CheckAndMarkExpiredAbsentSchedulesAsync()
        {
            try
            {
                var localNow = DateTime.UtcNow.AddHours(7);
                var expiredSchedules = (await _repo.GetExpiredUncheckedInSchedulesAsync()) ?? new List<WorkSchedule>();
                bool hasChanges = false;
                foreach (var ws in expiredSchedules)
                {
                    if (ws.Shift == null) continue;

                    var shiftEndLocal = ws.WorkDate.ToDateTime(ws.Shift.EndTime);
                    if (ws.Shift.EndTime < ws.Shift.StartTime)
                    {
                        shiftEndLocal = shiftEndLocal.AddDays(1);
                    }

                    if (shiftEndLocal <= localNow)
                    {
                        ws.Status = "ABSENT";
                        await _repo.UpdateAsync(ws);
                        hasChanges = true;
                    }
                }
                if (hasChanges)
                {
                    await _repo.SaveChangesAsync();
                }
            }
            catch
            {
                // Fail-safe
            }
        }

        public async Task<List<WorkScheduleViewDto>> GetAllAsync()
        {
            await CheckAndMarkExpiredAbsentSchedulesAsync();
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<WorkScheduleViewDto>>(data);
        }

        public async Task<WorkScheduleViewDto?> GetByIdAsync(long id)
        {
            await CheckAndMarkExpiredAbsentSchedulesAsync();
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return null;
            return _mapper.Map<WorkScheduleViewDto>(data);
        }

        public async Task<WorkScheduleViewDto> CreateAsync(WorkScheduleCreateDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            if (await _branchRepo.GetByIdAsync(dto.BranchId) == null)
            {
                throw new ArgumentException($"Branch with ID {dto.BranchId} does not exist.");
            }
            if (await _shiftRepo.GetByIdAsync(dto.ShiftId) == null)
            {
                throw new ArgumentException($"Shift with ID {dto.ShiftId} does not exist.");
            }
            var account = await _accountRepo.GetByIdAsync(dto.AccountId);
            if (account == null)
            {
                throw new ArgumentException($"Tài khoản với ID {dto.AccountId} không tồn tại.");
            }
            var activeContract = account.Contracts?
                .Where(c => c.BranchId == dto.BranchId && (c.Status == "Active" || c.Status == "Hiệu lực"))
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefault();

            if (activeContract == null)
            {
                throw new ArgumentException($"Nhân viên '{account.Name}' (ID: {dto.AccountId}) chưa có hợp đồng làm việc còn hiệu lực tại chi nhánh này.");
            }

            if (dto.WorkDate < activeContract.StartDate || (activeContract.EndDate.HasValue && dto.WorkDate > activeContract.EndDate.Value))
            {
                var endStr = activeContract.EndDate.HasValue ? activeContract.EndDate.Value.ToString("dd/MM/yyyy") : "Vô hạn";
                throw new ArgumentException($"Nhân viên '{account.Name}' chỉ được phân công ca làm việc trong thời hạn hợp đồng ({activeContract.StartDate:dd/MM/yyyy} - {endStr}). Ngày chọn ({dto.WorkDate:dd/MM/yyyy}) nằm ngoài thời hạn hợp đồng!");
            }

            if (await _repo.ExistsAsync(dto.AccountId, dto.ShiftId, dto.WorkDate))
            {
                throw new ArgumentException($"Nhân viên {account.Name} đã được phân công ca làm việc này vào ngày {dto.WorkDate:dd/MM/yyyy}.");
            }

            var shift = await _shiftRepo.GetByIdAsync(dto.ShiftId);
            if (shift != null)
            {
                ValidateShiftApplicability(shift, activeContract, account);
                if (await _repo.HasOverlappingScheduleAsync(dto.AccountId, dto.WorkDate, shift.StartTime, shift.EndTime))
                {
                    throw new ArgumentException($"Nhân viên {account.Name} đã có ca làm việc trùng giờ trong ngày {dto.WorkDate:dd/MM/yyyy}.");
                }
            }

            var entity = _mapper.Map<WorkSchedule>(dto);
            if (entity.CheckInAt.HasValue && entity.CheckInAt.Value.Kind == DateTimeKind.Unspecified)
            {
                entity.CheckInAt = DateTime.SpecifyKind(entity.CheckInAt.Value, DateTimeKind.Utc);
            }
            if (entity.CheckOutAt.HasValue && entity.CheckOutAt.Value.Kind == DateTimeKind.Unspecified)
            {
                entity.CheckOutAt = DateTime.SpecifyKind(entity.CheckOutAt.Value, DateTimeKind.Utc);
            }

            if (string.IsNullOrWhiteSpace(entity.Code))
            {
                entity.Code = $"WS-{dto.BranchId}-{dto.ShiftId}-{dto.AccountId}-{dto.WorkDate:yyyyMMdd}";
            }
            if (string.IsNullOrWhiteSpace(entity.Status) || entity.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                entity.Status = "Approved";
            }
            entity.ManagerApproved = true;
            entity.CreatedAt = DateTime.UtcNow;

            if (entity.IsHeadChef)
            {
                await ClearOtherHeadChefsAsync(entity.BranchId, entity.ShiftId, entity.WorkDate, excludeId: null);
            }

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            return _mapper.Map<WorkScheduleViewDto>(entity);
        }

        public async Task<List<WorkScheduleViewDto>> AssignBulkAsync(WorkScheduleAssignDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            if (await _branchRepo.GetByIdAsync(dto.BranchId) == null)
            {
                throw new ArgumentException($"Branch with ID {dto.BranchId} does not exist.");
            }
            if (await _shiftRepo.GetByIdAsync(dto.ShiftId) == null)
            {
                throw new ArgumentException($"Shift with ID {dto.ShiftId} does not exist.");
            }

            var validAccountIdsWithContracts = new List<long>();
            foreach (var accountId in dto.AccountIds)
            {
                var acc = await _accountRepo.GetByIdAsync(accountId);
                if (acc != null && acc.Contracts != null && acc.Contracts.Any(c => c.BranchId == dto.BranchId && c.Status == "Active"))
                {
                    validAccountIdsWithContracts.Add(accountId);
                }
            }

            if (validAccountIdsWithContracts.Count == 0)
            {
                throw new ArgumentException("Không có nhân viên nào trong danh sách được chọn có hợp đồng còn hiệu lực tại chi nhánh này.");
            }

            var createdSchedules = new List<WorkSchedule>();
            int totalValidContractDays = 0;

            foreach (var accountId in validAccountIdsWithContracts)
            {
                var acc = await _accountRepo.GetByIdAsync(accountId);
                var activeContract = acc?.Contracts?
                    .Where(c => c.BranchId == dto.BranchId && (c.Status == "Active" || c.Status == "Hiệu lực"))
                    .OrderByDescending(c => c.StartDate)
                    .FirstOrDefault();

                foreach (var workDate in dto.WorkDates)
                {
                    if (activeContract != null)
                    {
                        if (workDate < activeContract.StartDate || (activeContract.EndDate.HasValue && workDate > activeContract.EndDate.Value))
                        {
                            continue;
                        }
                    }

                    totalValidContractDays++;

                    if (await _repo.ExistsAsync(accountId, dto.ShiftId, workDate))
                    {
                        continue;
                    }

                    var shift = await _shiftRepo.GetByIdAsync(dto.ShiftId);
                    if (shift != null)
                    {
                        if (activeContract != null && !IsShiftApplicable(shift, activeContract))
                        {
                            continue;
                        }
                        if (await _repo.HasOverlappingScheduleAsync(accountId, workDate, shift.StartTime, shift.EndTime))
                        {
                            continue;
                        }
                    }

                    var schedule = new WorkSchedule
                    {
                        AccountId = accountId,
                        BranchId = dto.BranchId,
                        ShiftId = dto.ShiftId,
                        WorkDate = workDate,
                        Code = $"WS-{dto.BranchId}-{dto.ShiftId}-{accountId}-{workDate:yyyyMMdd}",
                        Status = "Approved",
                        ManagerApproved = true,
                        CreatedBy = dto.CreatedBy,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _repo.CreateAsync(schedule);
                    createdSchedules.Add(schedule);
                }
            }

            if (createdSchedules.Count == 0)
            {
                if (totalValidContractDays == 0)
                {
                    throw new ArgumentException("Tất cả các ngày phân ca được chọn đều nằm ngoài thời hạn hợp đồng của nhân viên! Chỉ được phân ca trong thời hạn hợp đồng.");
                }
                else
                {
                    throw new ArgumentException("Tất cả lịch làm việc trong thời hạn hợp đồng của nhân viên đã được phân công trước đó!");
                }
            }

            if (createdSchedules.Count > 0)
            {
                await _repo.SaveChangesAsync();
            }

            return _mapper.Map<List<WorkScheduleViewDto>>(createdSchedules);
        }

        public static decimal CalculateBoundedActualHours(WorkSchedule ws)
        {
            if (!ws.CheckInAt.HasValue || !ws.CheckOutAt.HasValue || ws.Shift == null)
                return 0;

            var shiftStartLocal = ws.WorkDate.ToDateTime(ws.Shift.StartTime);
            var shiftEndLocal = ws.WorkDate.ToDateTime(ws.Shift.EndTime);
            if (ws.Shift.EndTime < ws.Shift.StartTime)
            {
                shiftEndLocal = shiftEndLocal.AddDays(1);
            }

            var shiftStartUtc = DateTime.SpecifyKind(shiftStartLocal.AddHours(-7), DateTimeKind.Utc);
            var shiftEndUtc = DateTime.SpecifyKind(shiftEndLocal.AddHours(-7), DateTimeKind.Utc);

            var effectiveCheckIn = ws.CheckInAt.Value > shiftStartUtc ? ws.CheckInAt.Value : shiftStartUtc;
            var effectiveCheckOut = ws.CheckOutAt.Value < shiftEndUtc ? ws.CheckOutAt.Value : shiftEndUtc;

            if (effectiveCheckOut > effectiveCheckIn)
            {
                return (decimal)Math.Max(0, Math.Round((effectiveCheckOut - effectiveCheckIn).TotalHours, 2));
            }

            return 0;
        }

        public async Task<bool> UpdateAsync(WorkScheduleUpdateDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            var account = await _accountRepo.GetByIdAsync(dto.AccountId);
            if (account == null || account.Contracts == null || !account.Contracts.Any(c => c.BranchId == dto.BranchId && c.Status == "Active"))
            {
                throw new ArgumentException($"Nhân viên này chưa có hợp đồng làm việc còn hiệu lực tại chi nhánh này.");
            }

            if (entity.AccountId != dto.AccountId || entity.ShiftId != dto.ShiftId || entity.WorkDate != dto.WorkDate)
            {
                var allSchedules = await _repo.GetByAccountIdAsync(dto.AccountId);
                if (allSchedules.Any(ws => ws.Id != dto.Id && ws.ShiftId == dto.ShiftId && ws.WorkDate == dto.WorkDate))
                {
                    throw new ArgumentException($"Nhân viên {account.Name} đã được phân công ca làm việc này vào ngày {dto.WorkDate:dd/MM/yyyy}.");
                }
            }

            if (entity.ManagerApproved != dto.ManagerApproved)
            {
                var normStatus = (entity.Status ?? "").Trim().ToLower();
                if (normStatus == "working" || normStatus == "completed" || normStatus == "absent" || normStatus == "đang làm việc" || normStatus == "hoàn thành" || normStatus == "vắng mặt")
                {
                    throw new ArgumentException("Không thể sửa trạng thái Quản lý duyệt đối với ca làm việc đang ở trạng thái Đang làm việc, Hoàn thành hoặc Vắng mặt.");
                }
            }

            _mapper.Map(dto, entity);

            if (entity.IsHeadChef)
            {
                await ClearOtherHeadChefsAsync(entity.BranchId, entity.ShiftId, entity.WorkDate, excludeId: entity.Id);
            }

            if (dto.ApprovedOTHours.HasValue)
            {
                entity.ApprovedOTHours = dto.ApprovedOTHours.Value;
            }
            if (!string.IsNullOrWhiteSpace(dto.OTStatus))
            {
                entity.OTStatus = dto.OTStatus;
            }
            if (dto.OTNotes != null)
            {
                entity.OTNotes = dto.OTNotes;
            }

            if (entity.CheckInAt.HasValue && entity.CheckInAt.Value.Kind == DateTimeKind.Unspecified)
            {
                entity.CheckInAt = DateTime.SpecifyKind(entity.CheckInAt.Value, DateTimeKind.Utc);
            }
            if (entity.CheckOutAt.HasValue && entity.CheckOutAt.Value.Kind == DateTimeKind.Unspecified)
            {
                entity.CheckOutAt = DateTime.SpecifyKind(entity.CheckOutAt.Value, DateTimeKind.Utc);
            }

            if (entity.Shift == null && entity.ShiftId > 0)
            {
                entity.Shift = await _shiftRepo.GetByIdAsync(entity.ShiftId);
            }

            if (entity.CheckInAt.HasValue && entity.CheckOutAt.HasValue)
            {
                entity.ActualHours = CalculateBoundedActualHours(entity);
            }

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            // Báo cho nhân viên và các quản lý chi nhánh
            if (_hubContext?.Clients != null)
            {
                await _hubContext.Clients.Group($"user_{entity.AccountId}").SendAsync("ReceiveWorkScheduleNotification", new { message = "Lịch làm việc của bạn vừa được cập nhật." });
                await _hubContext.Clients.Group($"branch_{entity.BranchId}").SendAsync("ReceiveWorkScheduleNotification", new { message = "Lịch làm việc của chi nhánh vừa được cập nhật." });
            }

            return true;
        }

        // Mỗi ca (chi nhánh + ca + ngày) chỉ có tối đa 1 bếp trưởng — bỏ cờ của người khác
        // đang giữ trong cùng ca trước khi gán người mới.
        private async Task ClearOtherHeadChefsAsync(long branchId, long shiftId, DateOnly workDate, long? excludeId)
        {
            var others = await _context.WorkSchedules
                .Where(ws => ws.BranchId == branchId && ws.ShiftId == shiftId && ws.WorkDate == workDate
                          && ws.IsHeadChef && (excludeId == null || ws.Id != excludeId.Value))
                .ToListAsync();

            foreach (var other in others)
            {
                other.IsHeadChef = false;
            }
        }

        public async Task<bool> DeleteAsync(long id, string reason, long cancelledBy)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new InvalidOperationException("Phải nhập lý do hủy ca làm việc.");
            }

            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            // Ghi lại lý do hủy cho nhân viên biết vì bản ghi ca sẽ bị xóa hẳn —
            // đây là nơi duy nhất lưu lại lý do sau khi xóa.
            var shift = await _shiftRepo.GetByIdAsync(entity.ShiftId);
            var canceller = await _accountRepo.GetByIdAsync(cancelledBy);
            string shiftLabel = shift?.Name ?? "làm việc";
            string cancellerName = canceller?.Name ?? "Quản lý";
            string message = $"Ca {shiftLabel} ngày {entity.WorkDate:dd/MM/yyyy} của bạn đã bị {cancellerName} hủy. Lý do: {reason.Trim()}";

            var notification = new Notification
            {
                BranchId = entity.BranchId,
                AccountId = entity.AccountId,
                Type = "WorkSchedule",
                Message = message,
                IsImportant = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Notifications.AddAsync(notification);

            await _repo.DeleteAsync(id);
            await _repo.SaveChangesAsync();

            await _hubContext.Clients.Group($"user_{entity.AccountId}").SendAsync("ReceiveWorkScheduleNotification", new { message });

            return true;
        }

        public async Task<List<WorkScheduleViewDto>> GetByAccountIdAsync(long accountId)
        {
            await CheckAndMarkExpiredAbsentSchedulesAsync();
            var data = await _repo.GetByAccountIdAsync(accountId);
            return _mapper.Map<List<WorkScheduleViewDto>>(data);
        }
        public async Task<(bool Success, string Message, string Action)> RecordAttendanceAsync(
            long accountId, long branchId, long? workScheduleId = null)
        {
            var now = DateTime.UtcNow;
            var localNow = now.AddHours(7);
            var today = DateOnly.FromDateTime(localNow);
            var currentTimeOnly = TimeOnly.FromDateTime(localNow);
            
            var schedules = await _repo.GetTodaySchedulesAsync(accountId, branchId, today);
            
            if (schedules == null || !schedules.Any())
            {
                return (false, "Bạn không có ca làm việc hôm nay tại chi nhánh này.", "");
            }

            // Nếu nhân viên chỉ định ca cụ thể để điểm danh
            if (workScheduleId.HasValue && workScheduleId.Value > 0)
            {
                var targetWs = schedules.FirstOrDefault(s => s.Id == workScheduleId.Value) 
                               ?? await _repo.GetByIdAsync(workScheduleId.Value);

                if (targetWs == null || targetWs.AccountId != accountId)
                {
                    return (false, "Không tìm thấy ca làm việc đã chọn.", "");
                }

                // Nếu ca đang ở trạng thái Working -> Thực hiện Check-out (Không giới hạn giờ check-out)
                if (targetWs.Status == "Working")
                {
                    targetWs.CheckOutAt = now;
                    targetWs.ActualHours = CalculateBoundedActualHours(targetWs);
                    targetWs.Status = "Completed";
                    await _repo.UpdateAsync(targetWs);
                    await _repo.SaveChangesAsync();
                    
                    var shiftName = targetWs.Shift?.Name ?? "ca làm";
                    return (true, $"Check-out ca '{shiftName}' thành công!", "checkout");
                }
                else
                {
                    // Check-in ca này -> Kiểm tra quy tắc 30 phút
                    if (targetWs.Shift == null && targetWs.ShiftId > 0)
                    {
                        targetWs.Shift = await _shiftRepo.GetByIdAsync(targetWs.ShiftId);
                    }

                    var shiftName = targetWs.Shift?.Name ?? "ca làm";

                    if (targetWs.Shift != null)
                    {
                        var shiftStartLocal = targetWs.WorkDate.ToDateTime(targetWs.Shift.StartTime);
                        var shiftEndLocal = targetWs.WorkDate.ToDateTime(targetWs.Shift.EndTime);
                        if (targetWs.Shift.EndTime < targetWs.Shift.StartTime)
                        {
                            shiftEndLocal = shiftEndLocal.AddDays(1);
                        }

                        var earliestCheckIn = shiftStartLocal.AddMinutes(-30);

                        if (localNow < earliestCheckIn)
                        {
                            var startTimeStr = targetWs.Shift.StartTime.ToString("HH:mm");
                            var earliestStr = earliestCheckIn.ToString("HH:mm");
                            return (
                                false,
                                $"Chưa đến giờ điểm danh cho ca '{shiftName}' ({startTimeStr}). Bạn chỉ có thể điểm danh trước ca làm tối đa 30 phút (từ {earliestStr}).",
                                ""
                            );
                        }

                        if (localNow >= shiftEndLocal)
                        {
                            var endTimeStr = targetWs.Shift.EndTime.ToString("HH:mm");
                            return (
                                false,
                                $"Ca làm '{shiftName}' đã kết thúc lúc {endTimeStr}. Quá giờ làm việc, bạn không thể chấm công vào ca nữa.",
                                ""
                            );
                        }
                    }

                    targetWs.CheckInAt = now;
                    targetWs.Status = "Working";
                    await _repo.UpdateAsync(targetWs);
                    await _repo.SaveChangesAsync();

                    return (true, $"Check-in ca '{shiftName}' thành công!", "checkin");
                }
            }

            // Fallback: Tự động chọn ca nếu không truyền workScheduleId
            var workingIndex = schedules.FindIndex(s => s.Status == "Working");

            if (workingIndex >= 0)
            {
                // Có ca đang Working -> Tiến hành Check-out (Không giới hạn giờ)
                bool autoSplitOccurred = false;
                
                while (workingIndex >= 0 && workingIndex < schedules.Count - 1)
                {
                    var currentWs = schedules[workingIndex];
                    var nextWs = schedules[workingIndex + 1];
                    
                    if (currentTimeOnly >= nextWs.Shift.StartTime && currentTimeOnly >= currentWs.Shift.EndTime)
                    {
                        var localEndTime = today.ToDateTime(currentWs.Shift.EndTime);
                        var utcEndTime = DateTime.SpecifyKind(localEndTime.AddHours(-7), DateTimeKind.Utc);
                        
                        currentWs.CheckOutAt = utcEndTime;
                        currentWs.ActualHours = CalculateBoundedActualHours(currentWs);
                        currentWs.Status = "Completed";
                        await _repo.UpdateAsync(currentWs);
                        
                        var localStartTime = today.ToDateTime(nextWs.Shift.StartTime);
                        var utcStartTime = DateTime.SpecifyKind(localStartTime.AddHours(-7), DateTimeKind.Utc);
                        
                        nextWs.CheckInAt = utcStartTime;
                        nextWs.Status = "Working";
                        await _repo.UpdateAsync(nextWs);
                        
                        autoSplitOccurred = true;
                        workingIndex++;
                    }
                    else
                    {
                        break;
                    }
                }

                var finalWorking = schedules[workingIndex];
                finalWorking.CheckOutAt = now;
                finalWorking.ActualHours = CalculateBoundedActualHours(finalWorking);
                finalWorking.Status = "Completed";
                await _repo.UpdateAsync(finalWorking);
                
                await _repo.SaveChangesAsync();
                
                string msg = autoSplitOccurred 
                    ? "Đã tự động chốt các ca liên tiếp và Check-out thành công" 
                    : "Check-out thành công";
                    
                return (true, msg, "checkout");
            }
            else
            {
                var nextSchedule = schedules
                    .Where(s => s.CheckInAt == null && (s.Status == "Scheduled" || s.Status == "Approved" || s.Status == "Pending"))
                    .OrderBy(s => s.Shift.StartTime)
                    .FirstOrDefault(s =>
                    {
                        if (s.Shift == null) return false;
                        var shiftEndLocal = s.WorkDate.ToDateTime(s.Shift.EndTime);
                        if (s.Shift.EndTime < s.Shift.StartTime)
                        {
                            shiftEndLocal = shiftEndLocal.AddDays(1);
                        }
                        return localNow < shiftEndLocal;
                    });

                if (nextSchedule != null)
                {
                    // Kiểm tra quy tắc 30 phút trước ca làm
                    if (nextSchedule.Shift != null)
                    {
                        var shiftStartLocal = nextSchedule.WorkDate.ToDateTime(nextSchedule.Shift.StartTime);
                        var earliestCheckIn = shiftStartLocal.AddMinutes(-30);

                        if (localNow < earliestCheckIn)
                        {
                            var shiftName = nextSchedule.Shift.Name;
                            var startTimeStr = nextSchedule.Shift.StartTime.ToString("HH:mm");
                            var earliestStr = earliestCheckIn.ToString("HH:mm");
                            return (
                                false,
                                $"Chưa đến giờ điểm danh cho ca '{shiftName}' ({startTimeStr}). Bạn chỉ có thể điểm danh trước ca làm tối đa 30 phút (từ {earliestStr}).",
                                ""
                            );
                        }
                    }

                    var missedSchedules = schedules
                        .Where(s => s.CheckInAt == null 
                                 && (s.Status == "Scheduled" || s.Status == "Approved" || s.Status == "Pending")
                                 && s.Id != nextSchedule.Id
                                 && s.Shift.EndTime <= currentTimeOnly)
                        .ToList();

                    foreach (var missed in missedSchedules)
                    {
                        missed.Status = "Absent";
                        await _repo.UpdateAsync(missed);
                    }

                    nextSchedule.CheckInAt = now;
                    nextSchedule.Status = "Working";
                    await _repo.UpdateAsync(nextSchedule);
                    await _repo.SaveChangesAsync();
                    return (true, "Check-in thành công", "checkin");
                }
                else
                {
                    // Nếu có các ca chưa điểm danh hôm nay nhưng đã qua giờ kết thúc -> chuyển sang Absent
                    var expiredUncheckedSchedules = schedules
                        .Where(s => s.CheckInAt == null && (s.Status == "Scheduled" || s.Status == "Approved" || s.Status == "Pending"))
                        .ToList();

                    if (expiredUncheckedSchedules.Any())
                    {
                        foreach (var expired in expiredUncheckedSchedules)
                        {
                            expired.Status = "Absent";
                            await _repo.UpdateAsync(expired);
                        }
                        await _repo.SaveChangesAsync();

                        return (false, "Ca làm việc hôm nay của bạn đã kết thúc. Quá giờ làm việc, bạn không thể chấm công vào ca nữa.", "");
                    }

                    var lastCompleted = schedules.LastOrDefault(s => s.Status == "Completed");
                    if (lastCompleted != null)
                    {
                        lastCompleted.CheckOutAt = now;
                        lastCompleted.ActualHours = CalculateBoundedActualHours(lastCompleted);
                        await _repo.UpdateAsync(lastCompleted);
                        await _repo.SaveChangesAsync();
                        return (true, "Đã cập nhật giờ Check-out", "checkout");
                    }
                    
                    return (false, "Tất cả ca làm việc hôm nay đã hoàn thành.", "");
                }
            }
        }

        public async Task<List<long>> GetActiveRoleIdsAsync(long accountId)
        {
            return await _accountRepo.GetActiveRoleIdsAsync(accountId);
        }

        // ---- Hàm tiện ích tính khoảng cách Haversine (mét) ----
        private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000; // bán kính Trái Đất tính bằng mét
            var dLat = (lat2 - lat1) * Math.PI / 180.0;
            var dLon = (lon2 - lon1) * Math.PI / 180.0;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        public async Task<(bool Success, string Message, string Action)> CheckInWithLocationAsync(
            long accountId, long branchId, double latitude, double longitude, long? workScheduleId = null)
        {
            // 1. Kiểm tra xem ca làm việc này có phải là Check-out hay không trước khi kiểm tra GPS
            var localNow = DateTime.UtcNow.AddHours(7);
            var today = DateOnly.FromDateTime(localNow);
            var schedules = await _repo.GetTodaySchedulesAsync(accountId, branchId, today);

            bool isCheckOut = false;

            if (workScheduleId.HasValue && workScheduleId.Value > 0)
            {
                var targetWs = schedules?.FirstOrDefault(s => s.Id == workScheduleId.Value) 
                               ?? await _repo.GetByIdAsync(workScheduleId.Value);

                if (targetWs != null && targetWs.Status == "Working")
                {
                    isCheckOut = true;
                }
            }
            else if (schedules != null && schedules.Any(s => s.Status == "Working"))
            {
                isCheckOut = true;
            }

            // Nếu là Check-out -> Cho phép Check-out ngay không cần kiểm tra vị trí GPS!
            if (isCheckOut)
            {
                return await RecordAttendanceAsync(accountId, branchId, workScheduleId);
            }

            // 2. Lấy thông tin chi nhánh đối với lượt Check-in
            var branch = await _branchRepo.GetByIdAsync(branchId);
            if (branch == null)
                return (false, $"Không tìm thấy chi nhánh (ID: {branchId}).", "");

            // 3. Kiểm tra xem chi nhánh đã có tọa độ GPS chưa
            if (branch.Latitude == null || branch.Longitude == null
                || (branch.Latitude == 0 && branch.Longitude == 0))
            {
                // Nếu chưa cài đặt vị trí, bỏ qua kiểm tra và ghi nhận điểm danh bình thường
                return await RecordAttendanceAsync(accountId, branchId, workScheduleId);
            }

            // 4. Tính khoảng cách vị trí đối với lượt Check-in
            double distance = HaversineDistance(
                latitude, longitude,
                branch.Latitude.Value, branch.Longitude.Value);

            double radiusMeters = branch.CheckinRadiusMeters > 0 ? branch.CheckinRadiusMeters : 100;

            if (distance > radiusMeters)
            {
                int displayDist = (int)Math.Round(distance);
                int displayRadius = (int)radiusMeters;
                return (
                    false,
                    $"❗ Bạn đang ở cách nhà hàng ≈ {displayDist}m (bán kính cho phép: {displayRadius}m). " +
                    $"Vui lòng đến đúng địa điểm chi nhánh để điểm danh.",
                    ""
                );
            }

            // 5. Vị trí hợp lệ → ghi nhận điểm danh
            return await RecordAttendanceAsync(accountId, branchId, workScheduleId);
        }

        public async Task<bool> RequestLeaveAsync(long accountId, LeaveRequestDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.WorkScheduleId);
            if (entity == null || entity.AccountId != accountId) return false;

            var shift = await _shiftRepo.GetByIdAsync(entity.ShiftId);
            if (shift != null)
            {
                var localNow = DateTime.UtcNow.AddHours(7);
                var today = DateOnly.FromDateTime(localNow);
                var currentTime = TimeOnly.FromDateTime(localNow);

                if (entity.WorkDate < today || (entity.WorkDate == today && shift.EndTime <= currentTime))
                {
                    throw new InvalidOperationException("Không thể xin nghỉ ca làm việc đã kết thúc trong quá khứ.");
                }
            }

            var currentStatus = (entity.Status ?? "").ToUpper();
            if (currentStatus.StartsWith("LEAVE_REQUEST"))
            {
                throw new InvalidOperationException("Bạn đã gửi đơn xin nghỉ cho ca làm việc này rồi.");
            }
            if (currentStatus == "ABSENT" || currentStatus == "VẮNG MẶT" || currentStatus == "VẮNG")
            {
                throw new InvalidOperationException("Ca làm việc này đã được duyệt nghỉ.");
            }

            var reasonStr = string.IsNullOrWhiteSpace(dto.Reason) ? "Xin nghỉ ca" : dto.Reason;
            entity.Status = $"LEAVE_REQUEST:{reasonStr}";
            entity.ManagerApproved = false;

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            // Gửi thông báo đến quản lý chi nhánh
            await _hubContext.Clients.Group($"branch_{entity.BranchId}").SendAsync("ReceiveWorkScheduleNotification", new { message = "Có đơn xin nghỉ phép mới cần duyệt." });

            return true;
        }

        public async Task<bool> RequestShiftSwapAsync(long accountId, ShiftSwapRequestDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.WorkScheduleId);
            if (entity == null || entity.AccountId != accountId) return false;

            var targetAccount = await _accountRepo.GetByIdAsync(dto.TargetAccountId);
            if (targetAccount == null) return false;

            if (await _repo.ExistsAsync(dto.TargetAccountId, entity.ShiftId, entity.WorkDate))
            {
                throw new InvalidOperationException($"Nhân viên nhận ca ({targetAccount.Name}) đã được phân công ca làm việc này vào ngày {entity.WorkDate:dd/MM/yyyy}.");
            }

            var shift = await _shiftRepo.GetByIdAsync(entity.ShiftId);
            if (shift != null && await _repo.HasOverlappingScheduleAsync(dto.TargetAccountId, entity.WorkDate, shift.StartTime, shift.EndTime))
            {
                throw new InvalidOperationException($"Nhân viên nhận ca ({targetAccount.Name}) đã có ca làm việc trùng giờ trong ngày {entity.WorkDate:dd/MM/yyyy}.");
            }

            if (shift != null)
            {
                var localNow = DateTime.UtcNow.AddHours(7);
                var today = DateOnly.FromDateTime(localNow);
                var currentTime = TimeOnly.FromDateTime(localNow);

                if (entity.WorkDate < today || (entity.WorkDate == today && shift.EndTime <= currentTime))
                {
                    throw new InvalidOperationException("Không thể đổi ca làm việc đã kết thúc trong quá khứ.");
                }
            }

            var currentStatus = (entity.Status ?? "").ToUpper();
            if (currentStatus.StartsWith("LEAVE_REQUEST"))
            {
                throw new InvalidOperationException("Ca làm việc này đang chờ duyệt xin nghỉ, không thể đổi ca.");
            }
            if (currentStatus.StartsWith("TRANSFER_PENDING"))
            {
                throw new InvalidOperationException("Ca làm việc này đang trong quá trình chờ đổi ca.");
            }
            if (currentStatus == "ABSENT" || currentStatus == "VẮNG MẶT" || currentStatus == "VẮNG")
            {
                throw new InvalidOperationException("Ca làm việc này đã được duyệt nghỉ.");
            }

            var noteStr = string.IsNullOrWhiteSpace(dto.Note) ? "Đổi ca" : dto.Note;
            
            // Theo luồng FE, gán accountId của người được nhận ca cho ca làm việc
            // và giữ lại AccountId cũ trong Status
            var oldAccountId = entity.AccountId;
            entity.AccountId = dto.TargetAccountId;
            
            entity.Status = $"TRANSFER_PENDING_EMP:{oldAccountId}:{noteStr}";
            entity.ManagerApproved = false;

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            // Gửi thông báo cho nhân viên được nhờ nhận ca và quản lý
            var senderAccount = await _accountRepo.GetByIdAsync(accountId);
            string senderName = senderAccount != null ? senderAccount.Name : "Một nhân viên";
            var notification = new Notification
            {
                BranchId = entity.BranchId,
                AccountId = dto.TargetAccountId,
                Type = "WorkSchedule",
                Message = $"{senderName} đã gửi cho bạn một lời mời nhận ca làm việc.",
                IsImportant = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Notifications.AddAsync(notification);


            await _hubContext.Clients.Group($"user_{dto.TargetAccountId}").SendAsync("ReceiveWorkScheduleNotification", new { message = "Bạn có lời mời nhận ca làm việc mới." });
            await _hubContext.Clients.Group($"branch_{entity.BranchId}").SendAsync("ReceiveWorkScheduleNotification", new { message = "Có yêu cầu đổi ca mới đang được xử lý." });

            return true;
        }

        // Quản lý từ chối/hủy yêu cầu đổi ca giữa 2 nhân viên — trả ca về đúng người
        // đăng ký ban đầu và bắt buộc ghi lý do để thông báo cho cả 2 bên (không có
        // cột lưu riêng nên gửi qua Notification, giống luồng hủy ca làm việc).
        public async Task<bool> RejectShiftSwapAsync(long workScheduleId, long senderAccountId, string note, long rejectedBy)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                throw new InvalidOperationException("Phải nhập lý do từ chối/hủy yêu cầu đổi ca.");
            }

            var entity = await _repo.GetByIdAsync(workScheduleId);
            if (entity == null) return false;

            var receiverAccountId = entity.AccountId;
            var canceller = await _accountRepo.GetByIdAsync(rejectedBy);
            string cancellerName = canceller?.Name ?? "Quản lý";
            string message = $"Yêu cầu đổi ca ngày {entity.WorkDate:dd/MM/yyyy} đã bị {cancellerName} từ chối/hủy. Lý do: {note.Trim()}";

            entity.AccountId = senderAccountId;
            entity.Status = "APPROVED";
            entity.ManagerApproved = true;

            var notifyIds = new List<long> { senderAccountId };
            if (receiverAccountId != senderAccountId) notifyIds.Add(receiverAccountId);

            foreach (var accId in notifyIds)
            {
                await _context.Notifications.AddAsync(new Notification
                {
                    BranchId = entity.BranchId,
                    AccountId = accId,
                    Type = "WorkSchedule",
                    Message = message,
                    IsImportant = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            foreach (var accId in notifyIds)
            {
                await _hubContext.Clients.Group($"user_{accId}").SendAsync("ReceiveWorkScheduleNotification", new { message });
            }

            return true;
        }

        public async Task<bool> ApproveOTAsync(WorkScheduleOTApproveDto dto)
        {
            var ws = await _repo.GetByIdAsync(dto.WorkScheduleId);
            if (ws == null) return false;

            ws.ApprovedOTHours = dto.ApprovedOTHours;
            ws.OTStatus = string.IsNullOrWhiteSpace(dto.OTStatus) ? "Approved" : dto.OTStatus;
            ws.OTNotes = dto.OTNotes ?? string.Empty;

            await _repo.UpdateAsync(ws);
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<WorkSchedulePOSActivityDto> VerifyPOSActivityAsync(long workScheduleId)
        {
            var ws = await _repo.GetByIdAsync(workScheduleId);
            if (ws == null) throw new KeyNotFoundException($"Không tìm thấy lịch làm việc ID {workScheduleId}");

            var account = await _accountRepo.GetByIdAsync(ws.AccountId);
            var accountName = account?.Name ?? $"ID {ws.AccountId}";

            decimal actualHours = ws.ActualHours ?? 0m;
            decimal standardHours = ws.Shift != null ? ws.Shift.StandardHours : 8.0m;
            if (standardHours <= 0) standardHours = 8.0m;

            decimal excessHours = Math.Max(0m, actualHours - standardHours);

            var result = new WorkSchedulePOSActivityDto
            {
                WorkScheduleId = ws.Id,
                AccountId = ws.AccountId,
                AccountName = accountName,
                WorkDate = ws.WorkDate,
                CheckInAt = ws.CheckInAt,
                CheckOutAt = ws.CheckOutAt,
                ActualHours = actualHours,
                StandardHours = standardHours,
                ExcessHours = excessHours,
                OrderCountDuringExcessTime = 0,
                TotalOrderAmountDuringExcessTime = 0m,
                HasPOSActivity = false,
                SuspicionReason = ""
            };

            if (excessHours <= 0 || !ws.CheckInAt.HasValue || !ws.CheckOutAt.HasValue)
            {
                result.SuspicionReason = "Không có số giờ dư ra so với ca tiêu chuẩn.";
                return result;
            }

            // Expected end of standard shift
            var expectedEndTime = ws.CheckInAt.Value.AddHours((double)standardHours);
            var actualCheckOutTime = ws.CheckOutAt.Value;

            if (actualCheckOutTime <= expectedEndTime)
            {
                result.SuspicionReason = "Thời gian check-out nằm trong khung giờ ca chuẩn.";
                return result;
            }

            // Query orders created by this staff account during the excess time interval
            var orders = await _context.Orders
                .Where(o => o.CreatedBy == ws.AccountId 
                         && o.CreatedAt >= expectedEndTime 
                         && o.CreatedAt <= actualCheckOutTime)
                .ToListAsync();

            result.OrderCountDuringExcessTime = orders.Count;
            result.TotalOrderAmountDuringExcessTime = orders.Sum(o => o.TotalAmount);
            result.HasPOSActivity = orders.Count > 0;

            if (!result.HasPOSActivity)
            {
                result.SuspicionReason = $"Không ghi nhận bất kỳ đơn POS/Order nào từ {expectedEndTime:HH:mm dd/MM} đến {actualCheckOutTime:HH:mm dd/MM}. Có thể nhân viên đã quên check-out khi về.";
            }
            else
            {
                result.SuspicionReason = $"Xác nhận có phát sinh {orders.Count} đơn POS trong thời gian dư ({result.TotalOrderAmountDuringExcessTime:N0}đ). Có khả năng làm tăng ca (OT) thật.";
            }

            return result;
        }

        private static bool IsShiftApplicable(Shift shift, Contract contract)
        {
            if (shift == null || contract == null) return true;
            var shiftApp = (shift.ApplicableTo ?? "ALL").ToUpperInvariant();
            if (shiftApp == "ALL") return true;

            bool isHourly = string.Equals(contract.SalaryType, "Hourly", StringComparison.OrdinalIgnoreCase);
            if (shiftApp == "FULL_TIME" && isHourly) return false;
            if (shiftApp == "PART_TIME" && !isHourly) return false;
            return true;
        }

        private static void ValidateShiftApplicability(Shift shift, Contract contract, Account account)
        {
            if (shift == null || contract == null || account == null) return;
            var shiftApp = (shift.ApplicableTo ?? "ALL").ToUpperInvariant();
            if (shiftApp == "ALL") return;

            bool isHourly = string.Equals(contract.SalaryType, "Hourly", StringComparison.OrdinalIgnoreCase);
            if (shiftApp == "FULL_TIME" && isHourly)
            {
                throw new ArgumentException($"Ca làm việc '{shift.Name}' chỉ áp dụng cho nhân viên Toàn thời gian. Nhân viên '{account.Name}' là Bán thời gian!");
            }
            if (shiftApp == "PART_TIME" && !isHourly)
            {
                throw new ArgumentException($"Ca làm việc '{shift.Name}' chỉ áp dụng cho nhân viên Bán thời gian. Nhân viên '{account.Name}' là Toàn thời gian!");
            }
        }

        public async Task<WorkScheduleAutoAssignResultDto> AutoAssignAsync(WorkScheduleAutoAssignDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.StartDate > dto.EndDate)
                throw new ArgumentException("Ngày bắt đầu không được lớn hơn ngày kết thúc.");

            var branch = await _branchRepo.GetByIdAsync(dto.BranchId);
            if (branch == null)
                throw new ArgumentException($"Branch with ID {dto.BranchId} does not exist.");

            // 1. Get active shifts for this branch with role requirements
            var shiftsQuery = _context.Shifts
                .Include(s => s.RoleRequirements)
                .Where(s => s.IsActive);
            if (dto.ShiftIds != null && dto.ShiftIds.Count > 0)
            {
                shiftsQuery = shiftsQuery.Where(s => dto.ShiftIds.Contains(s.Id));
            }
            var activeShifts = await shiftsQuery.ToListAsync();
            if (!activeShifts.Any())
            {
                throw new ArgumentException("Không tìm thấy ca làm việc nào áp dụng.");
            }

            // 2. Get active contracts in branch
            var activeContracts = await _context.Contracts
                .Include(c => c.Account)
                .Where(c => c.BranchId == dto.BranchId && (c.Status == "Active" || c.Status == "Hiệu lực"))
                .ToListAsync();

            if (!activeContracts.Any())
            {
                throw new ArgumentException("Không có nhân viên nào có hợp đồng còn hiệu lực tại chi nhánh này.");
            }

            // Distinct role IDs present among active contracts in this branch
            var branchRoleIds = activeContracts
                .Select(c => c.RoleId)
                .Distinct()
                .ToList();

            // 3. Get existing schedules in the date range & existing count in the month
            var startMonthDate = new DateOnly(dto.StartDate.Year, dto.StartDate.Month, 1);
            var endMonthDate = new DateOnly(dto.EndDate.Year, dto.EndDate.Month, DateTime.DaysInMonth(dto.EndDate.Year, dto.EndDate.Month));

            var monthSchedules = await _context.WorkSchedules
                .Where(w => w.BranchId == dto.BranchId && w.WorkDate >= startMonthDate && w.WorkDate <= endMonthDate)
                .ToListAsync();

            var leaveRequests = await _context.WorkSchedules
                .Where(w => w.BranchId == dto.BranchId 
                         && w.WorkDate >= dto.StartDate 
                         && w.WorkDate <= dto.EndDate
                         && w.Status.StartsWith("LEAVE_APPROVED"))
                .ToListAsync();

            var leaveSet = new HashSet<(long AccountId, DateOnly WorkDate)>(
                leaveRequests.Select(l => (l.AccountId, l.WorkDate))
            );

            // Existing schedules set for date range
            var rangeSchedules = monthSchedules
                .Where(w => w.WorkDate >= dto.StartDate && w.WorkDate <= dto.EndDate)
                .ToList();

            var existingScheduleSet = new HashSet<(long AccountId, DateOnly WorkDate)>(
                rangeSchedules.Select(w => (w.AccountId, w.WorkDate))
            );

            // Group staff by Full-Time and Part-Time
            var fullTimeStaff = activeContracts
                .Where(c => string.Equals(c.Type, "Full-time", StringComparison.OrdinalIgnoreCase) || !string.Equals(c.SalaryType, "Hourly", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var partTimeStaff = activeContracts
                .Where(c => string.Equals(c.Type, "Part-time", StringComparison.OrdinalIgnoreCase) || string.Equals(c.SalaryType, "Hourly", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Track scheduled days per staff in current month
            var staffMonthlyWorkDays = new Dictionary<long, int>();
            foreach (var c in activeContracts)
            {
                var count = monthSchedules.Count(m => m.AccountId == c.AccountId && !m.Status.StartsWith("ABSENT"));
                staffMonthlyWorkDays[c.AccountId] = count;
            }

            var result = new WorkScheduleAutoAssignResultDto();
            var newSchedules = new List<WorkSchedule>();

            // Iterate each date in date range
            for (var date = dto.StartDate; date <= dto.EndDate; date = date.AddDays(1))
            {
                foreach (var shift in activeShifts)
                {
                    string shiftApplicableRule = (shift.ApplicableTo ?? "ALL").ToUpperInvariant();

                    // Determine required role slots for this shift
                    var roleSlots = new List<(long RoleId, int MinQuantity)>();
                    if (shift.RoleRequirements != null && shift.RoleRequirements.Any())
                    {
                        foreach (var req in shift.RoleRequirements)
                        {
                            if (req.MinQuantity > 0)
                            {
                                roleSlots.Add((req.RoleId, req.MinQuantity));
                            }
                        }
                    }
                    else
                    {
                        // Default: 1 slot per distinct staff role in branch
                        foreach (var rId in branchRoleIds)
                        {
                            roleSlots.Add((rId, 1));
                        }
                    }

                    // Fill slots for each required role
                    foreach (var (requiredRoleId, minQty) in roleSlots)
                    {
                        for (int q = 0; q < minQty; q++)
                        {
                            // Check Full-Time candidates matching requiredRoleId
                            var eligibleFullTime = fullTimeStaff.Where(c =>
                                c.RoleId == requiredRoleId &&
                                c.StartDate <= date &&
                                (!c.EndDate.HasValue || c.EndDate.Value >= date) &&
                                !leaveSet.Contains((c.AccountId, date)) &&
                                !existingScheduleSet.Contains((c.AccountId, date)) &&
                                !newSchedules.Any(ns => ns.AccountId == c.AccountId && ns.WorkDate == date)
                            ).ToList();

                            // Check Part-Time candidates matching requiredRoleId
                            var eligiblePartTime = partTimeStaff.Where(c =>
                                c.RoleId == requiredRoleId &&
                                c.StartDate <= date &&
                                (!c.EndDate.HasValue || c.EndDate.Value >= date) &&
                                !leaveSet.Contains((c.AccountId, date)) &&
                                !existingScheduleSet.Contains((c.AccountId, date)) &&
                                !newSchedules.Any(ns => ns.AccountId == c.AccountId && ns.WorkDate == date)
                            ).ToList();

                            Contract? chosenContract = null;
                            bool isFullTimeAssigned = false;

                            if (shiftApplicableRule == "FULL_TIME" || shiftApplicableRule == "ALL")
                            {
                                // Prioritize Full-time employee furthest below standard work days target
                                var candidate = eligibleFullTime
                                    .Where(c => staffMonthlyWorkDays[c.AccountId] < (c.BaseWorkDay ?? 26))
                                    .OrderBy(c => staffMonthlyWorkDays[c.AccountId])
                                    .FirstOrDefault();

                                if (candidate == null && shiftApplicableRule == "FULL_TIME")
                                {
                                    candidate = eligibleFullTime.OrderBy(c => staffMonthlyWorkDays[c.AccountId]).FirstOrDefault();
                                }

                                if (candidate != null)
                                {
                                    chosenContract = candidate;
                                    isFullTimeAssigned = true;
                                }
                            }

                            if (chosenContract == null && (shiftApplicableRule == "PART_TIME" || shiftApplicableRule == "ALL"))
                            {
                                // Distribute evenly among eligible part-time staff
                                chosenContract = eligiblePartTime
                                    .OrderBy(c => staffMonthlyWorkDays[c.AccountId])
                                    .FirstOrDefault();
                            }

                            if (chosenContract != null)
                            {
                                var ws = new WorkSchedule
                                {
                                    AccountId = chosenContract.AccountId,
                                    BranchId = dto.BranchId,
                                    ShiftId = shift.Id,
                                    WorkDate = date,
                                    Code = $"WS-{dto.BranchId}-{shift.Id}-{chosenContract.AccountId}-{date:yyyyMMdd}",
                                    Status = "Approved",
                                    ManagerApproved = true,
                                    CreatedBy = dto.CreatedBy,
                                    CreatedAt = DateTime.UtcNow
                                };
                                newSchedules.Add(ws);
                                existingScheduleSet.Add((chosenContract.AccountId, date));
                                staffMonthlyWorkDays[chosenContract.AccountId] += 1;

                                if (isFullTimeAssigned) result.FullTimeSchedulesCreated++;
                                else result.PartTimeSchedulesCreated++;
                                result.TotalCreated++;
                            }
                            else
                            {
                                result.SkippedSlotsCount++;
                            }
                        }
                    }
                }
            }

            if (newSchedules.Any())
            {
                await _context.WorkSchedules.AddRangeAsync(newSchedules);
                await _context.SaveChangesAsync();
            }

            if (result.TotalCreated == 0)
            {
                result.Warnings.Add("Không có ca làm việc mới nào được tạo (do nhân viên đã có lịch hoặc không đáp ứng thời hạn hợp đồng/nghỉ phép).");
            }

            return result;
        }

        public async Task<List<MenuGoBE.Dtos.WorkSchedule.WorkScheduleActivityLogDto>> GetActivityLogAsync(long workScheduleId)
        {
            var ws = await _repo.GetByIdWithDetailsAsync(workScheduleId);

            if (ws == null || !ws.CheckInAt.HasValue)
            {
                return new List<MenuGoBE.Dtos.WorkSchedule.WorkScheduleActivityLogDto>();
            }

            // Chỉ hỗ trợ role Phục vụ (role = 6) - Mặc định cho chức năng này
            var roles = await _accountRepo.GetActiveRoleIdsAsync(ws.AccountId);
            var hasWaiterRole = roles.Contains(6);
            if (!hasWaiterRole)
            {
                return new List<MenuGoBE.Dtos.WorkSchedule.WorkScheduleActivityLogDto>();
            }

            var startTime = ws.CheckInAt.Value;
            // Tính toán EndTime: Nếu chưa CheckOut thì là min của hiện tại hoặc giờ kết thúc ca + 4 tiếng
            var endTime = ws.CheckOutAt ?? DateTime.UtcNow;
            if (!ws.CheckOutAt.HasValue && ws.Shift != null)
            {
                // Giả định ws.WorkDate là ngày của ca làm việc
                var shiftEndTime = ws.WorkDate.ToDateTime(ws.Shift.EndTime);
                var maxEndTime = shiftEndTime.AddHours(4);
                if (maxEndTime < DateTime.UtcNow)
                {
                    endTime = maxEndTime;
                }
            }

            var assignments = await _orderAssignmentRepo.GetActivityLogAssignmentsAsync(ws.AccountId, startTime, endTime);

            var logs = new List<MenuGoBE.Dtos.WorkSchedule.WorkScheduleActivityLogDto>();
            foreach (var a in assignments)
            {
                string actionName = a.AssignmentType == "Primary" ? "Nhận bàn chính" : "Hỗ trợ bàn";
                logs.Add(new MenuGoBE.Dtos.WorkSchedule.WorkScheduleActivityLogDto
                {
                    Timestamp = a.AssignedAt,
                    Action = actionName,
                    OrderCode = $"HD{a.OrderId}",
                    TableName = a.Order?.Table?.Name ?? "Mang về",
                    Details = $"{actionName} cho đơn hàng HD{a.OrderId} (Bàn: {a.Order?.Table?.Name ?? "Mang về"})"
                });
            }

            return logs;
        }
    }
}
