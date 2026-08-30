using MenuGoBE.Dtos.WorkSchedule;
using MenuGoBE.Exceptions;
using MenuGoBE.Hubs;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.AspNetCore.SignalR;

namespace MenuGoBE.Service
{
    public class ShiftChangeRequestService : IShiftChangeRequestService
    {
        private static readonly TimeOnly NightStart = new(22, 0);
        private static readonly TimeOnly NightEnd = new(6, 0);

        private readonly IShiftChangeRequestRepository _repo;
        private readonly IWorkScheduleRepository _workScheduleRepo;
        private readonly IShiftRepository _shiftRepo;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ShiftChangeRequestService(
            IShiftChangeRequestRepository repo,
            IWorkScheduleRepository workScheduleRepo,
            IShiftRepository shiftRepo,
            IHubContext<NotificationHub> hubContext)
        {
            _repo = repo;
            _workScheduleRepo = workScheduleRepo;
            _shiftRepo = shiftRepo;
            _hubContext = hubContext;
        }

        private static bool IsNightShift(TimeOnly start, TimeOnly end)
        {
            // Vắt qua khung giờ đêm (22:00 - 06:00): bắt đầu trong khung đêm,
            // kết thúc trong khung đêm, hoặc ca qua đêm (end < start).
            return start >= NightStart || start < NightEnd || end > NightStart || end <= NightEnd || end < start;
        }

        public async Task<ShiftChangeRequestViewDto> CreateAsync(long accountId, ShiftChangeRequestCreateDto dto)
        {
            var schedule = await _workScheduleRepo.GetByIdAsync(dto.WorkScheduleId);
            if (schedule == null || schedule.AccountId != accountId)
                throw new KeyNotFoundException("Không tìm thấy lịch làm việc.");

            var oldShift = await _shiftRepo.GetByIdAsync(schedule.ShiftId);
            if (oldShift == null)
                throw new InvalidOperationException("Không tìm thấy ca làm việc hiện tại.");

            // Không cho đổi ca đã kết thúc trong quá khứ
            var localNow = DateTime.UtcNow.AddHours(7);
            var today = DateOnly.FromDateTime(localNow);
            var currentTime = TimeOnly.FromDateTime(localNow);
            if (schedule.WorkDate < today || (schedule.WorkDate == today && oldShift.EndTime <= currentTime))
            {
                throw new InvalidOperationException("Không thể đổi ca làm việc đã kết thúc trong quá khứ.");
            }

            var currentStatus = (schedule.Status ?? string.Empty).ToUpper();
            if (currentStatus.StartsWith("LEAVE_REQUEST") || currentStatus.StartsWith("TRANSFER_PENDING") || currentStatus == "SHIFT_CHANGE_PENDING")
            {
                throw new InvalidOperationException("Ca làm việc này đang chờ xử lý một yêu cầu khác.");
            }
            if (await _repo.HasPendingForScheduleAsync(dto.WorkScheduleId))
            {
                throw new InvalidOperationException("Bạn đã gửi yêu cầu đổi ca cho lịch làm việc này rồi.");
            }

            // Resolve ca mới: chọn ca mẫu, hoặc tạo/tái sử dụng 1 Shift theo giờ tùy chỉnh
            Shift newShift;
            if (dto.NewShiftId.HasValue)
            {
                var chosen = await _shiftRepo.GetByIdAsync(dto.NewShiftId.Value);
                if (chosen == null || !chosen.IsActive)
                    throw new InvalidOperationException("Ca mẫu được chọn không hợp lệ.");
                newShift = chosen;
            }
            else if (dto.NewStartTime.HasValue && dto.NewEndTime.HasValue)
            {
                var existing = await _shiftRepo.GetByTimeAsync(dto.NewStartTime.Value, dto.NewEndTime.Value);
                if (existing != null)
                {
                    newShift = existing;
                }
                else
                {
                    newShift = new Shift
                    {
                        Name = $"Ca {dto.NewStartTime.Value:HH\\:mm}-{dto.NewEndTime.Value:HH\\:mm} (tùy chỉnh)",
                        StartTime = dto.NewStartTime.Value,
                        EndTime = dto.NewEndTime.Value,
                        IsActive = true,
                        CreatedBy = accountId,
                    };
                    await _shiftRepo.CreateAsync(newShift);
                    await _shiftRepo.SaveChangesAsync();
                }
            }
            else
            {
                throw new InvalidOperationException("Phải chọn ca mẫu hoặc nhập giờ ca mới.");
            }

            if (newShift.Id == oldShift.Id)
            {
                throw new InvalidOperationException("Ca mới phải khác ca hiện tại.");
            }

            if (await _workScheduleRepo.HasOverlappingScheduleAsync(accountId, schedule.WorkDate, newShift.StartTime, newShift.EndTime))
            {
                throw new InvalidOperationException($"Bạn đã có ca làm việc trùng giờ trong ngày {schedule.WorkDate:dd/MM/yyyy}.");
            }

            var entity = new ShiftChangeRequest
            {
                WorkScheduleId = schedule.Id,
                AccountId = accountId,
                BranchId = schedule.BranchId,
                WorkDate = schedule.WorkDate,
                OldShiftId = oldShift.Id,
                OldStartTime = oldShift.StartTime,
                OldEndTime = oldShift.EndTime,
                OldStatus = schedule.Status,
                OldManagerApproved = schedule.ManagerApproved,
                NewShiftId = newShift.Id,
                NewStartTime = newShift.StartTime,
                NewEndTime = newShift.EndTime,
                IsNightShift = IsNightShift(newShift.StartTime, newShift.EndTime),
                Reason = dto.Reason,
                Status = ShiftChangeRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
            };

            // Gán sẵn navigation trong bộ nhớ để dựng DTO trả về ngay, không cần
            // round-trip đọc lại (entity vừa tạo có thể chưa được EF gán quan hệ đầy đủ).
            entity.OldShift = oldShift;
            entity.NewShift = newShift;

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            schedule.Status = "SHIFT_CHANGE_PENDING";
            schedule.ManagerApproved = false;
            await _workScheduleRepo.UpdateAsync(schedule);
            await _workScheduleRepo.SaveChangesAsync();

            try
            {
                await _hubContext.Clients.Group($"branch_{schedule.BranchId}")
                    .SendAsync("ReceiveWorkScheduleNotification", new { message = "Có yêu cầu đổi ca mới cần duyệt." });
            }
            catch
            {
                // Không chặn request nếu SignalR lỗi
            }

            return MapToDto(entity);
        }

        public async Task<List<ShiftChangeRequestViewDto>> GetByBranchAsync(long branchId, ShiftChangeRequestStatus? status)
        {
            var data = await _repo.GetByBranchAsync(branchId, status);
            return data.Select(MapToDto).ToList();
        }

        public async Task<List<ShiftChangeRequestViewDto>> GetMineAsync(long accountId)
        {
            var data = await _repo.GetByAccountIdAsync(accountId);
            return data.Select(MapToDto).ToList();
        }

        public async Task<bool> ApproveAsync(long requestId, long approverId)
        {
            var request = await _repo.GetByIdAsync(requestId);
            if (request == null || request.Status != ShiftChangeRequestStatus.Pending) return false;

            var schedule = await _workScheduleRepo.GetByIdAsync(request.WorkScheduleId);
            if (schedule == null) return false;

            schedule.ShiftId = request.NewShiftId;
            schedule.Status = "Approved";
            schedule.ManagerApproved = true;
            await _workScheduleRepo.UpdateAsync(schedule);
            await _workScheduleRepo.SaveChangesAsync();

            // LƯU Ý: hệ thống lương hiện chưa có hệ số đêm/lễ/Tết/OT — khi tính năng
            // đó được xây dựng, đây là nơi cần trigger tính lại theo request.IsNightShift.
            request.Status = ShiftChangeRequestStatus.Approved;
            request.ApprovedBy = approverId;
            request.ApprovedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(request);
            await _repo.SaveChangesAsync();

            try
            {
                await _hubContext.Clients.Group($"branch_{request.BranchId}")
                    .SendAsync("ReceiveWorkScheduleNotification", new { message = "Yêu cầu đổi ca đã được duyệt." });
            }
            catch { }

            return true;
        }

        public async Task<bool> RejectAsync(long requestId, long approverId, string? reason)
        {
            var request = await _repo.GetByIdAsync(requestId);
            if (request == null || request.Status != ShiftChangeRequestStatus.Pending) return false;

            var schedule = await _workScheduleRepo.GetByIdAsync(request.WorkScheduleId);
            if (schedule != null)
            {
                schedule.Status = request.OldStatus;
                schedule.ManagerApproved = request.OldManagerApproved;
                await _workScheduleRepo.UpdateAsync(schedule);
                await _workScheduleRepo.SaveChangesAsync();
            }

            request.Status = ShiftChangeRequestStatus.Rejected;
            request.ApprovedBy = approverId;
            request.ApprovedAt = DateTime.UtcNow;
            request.RejectReason = reason;
            await _repo.UpdateAsync(request);
            await _repo.SaveChangesAsync();

            return true;
        }

        private static ShiftChangeRequestViewDto MapToDto(ShiftChangeRequest r)
        {
            return new ShiftChangeRequestViewDto
            {
                Id = r.Id,
                WorkScheduleId = r.WorkScheduleId,
                AccountId = r.AccountId,
                AccountName = r.Account?.Name ?? string.Empty,
                BranchId = r.BranchId,
                WorkDate = r.WorkDate,
                OldShiftId = r.OldShiftId,
                OldShiftName = r.OldShift?.Name ?? string.Empty,
                OldStartTime = r.OldStartTime,
                OldEndTime = r.OldEndTime,
                NewShiftId = r.NewShiftId,
                NewShiftName = r.NewShift?.Name ?? string.Empty,
                NewStartTime = r.NewStartTime,
                NewEndTime = r.NewEndTime,
                IsNightShift = r.IsNightShift,
                Reason = r.Reason,
                Status = r.Status,
                RequestedAt = r.RequestedAt,
                ApprovedBy = r.ApprovedBy,
                ApproverName = r.Approver?.Name,
                ApprovedAt = r.ApprovedAt,
                RejectReason = r.RejectReason,
            };
        }
    }
}
