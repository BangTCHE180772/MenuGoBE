using MenuGoBE.Dtos.WorkSchedule;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Service;

public class ShiftFeedbackService : IShiftFeedbackService
{
    private readonly IShiftFeedbackRepository _feedbackRepository;
    private readonly IWorkScheduleRepository _workScheduleRepository;

    public ShiftFeedbackService(
        IShiftFeedbackRepository feedbackRepository,
        IWorkScheduleRepository workScheduleRepository)
    {
        _feedbackRepository = feedbackRepository;
        _workScheduleRepository = workScheduleRepository;
    }

    public async Task<ShiftFeedbackViewDto> CreateFeedbackAsync(long accountId, ShiftFeedbackCreateDto dto)
    {
        var schedule = await _workScheduleRepository.GetByIdAsync(dto.WorkScheduleId);
        if (schedule == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy ca làm việc với ID: {dto.WorkScheduleId}");
        }

        if (schedule.AccountId != accountId)
        {
            throw new InvalidOperationException("Bạn chỉ có thể gửi phản hồi cho ca làm việc của chính mình.");
        }

        var feedback = new WorkScheduleFeedback
        {
            WorkScheduleId = dto.WorkScheduleId,
            AccountId = accountId,
            BranchId = schedule.BranchId,
            FeedbackType = string.IsNullOrWhiteSpace(dto.FeedbackType) ? "OTDispute" : dto.FeedbackType,
            Title = dto.Title.Trim(),
            Content = dto.Content.Trim(),
            Status = ShiftFeedbackStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _feedbackRepository.CreateAsync(feedback);
        await _feedbackRepository.SaveChangesAsync();

        var reloaded = await _feedbackRepository.GetByIdAsync(feedback.Id);
        return MapToViewDto(reloaded ?? feedback);
    }

    public async Task<List<ShiftFeedbackViewDto>> GetMyFeedbacksAsync(long accountId)
    {
        var list = await _feedbackRepository.GetByAccountIdAsync(accountId);
        return list.Select(MapToViewDto).ToList();
    }

    public async Task<List<ShiftFeedbackViewDto>> GetBranchFeedbacksAsync(long branchId, string? statusStr)
    {
        ShiftFeedbackStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(statusStr) && Enum.TryParse<ShiftFeedbackStatus>(statusStr, true, out var parsedStatus))
        {
            statusFilter = parsedStatus;
        }

        var list = await _feedbackRepository.GetByBranchAsync(branchId, statusFilter);
        return list.Select(MapToViewDto).ToList();
    }

    public async Task<ShiftFeedbackViewDto> ProcessFeedbackAsync(long feedbackId, long managerId, ShiftFeedbackProcessDto dto)
    {
        var feedback = await _feedbackRepository.GetByIdAsync(feedbackId);
        if (feedback == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy phản hồi với ID: {feedbackId}");
        }

        if (!Enum.TryParse<ShiftFeedbackStatus>(dto.Status, true, out var newStatus))
        {
            throw new ArgumentException("Trạng thái xử lý không hợp lệ (Resolved | Rejected)");
        }

        feedback.Status = newStatus;
        feedback.ResponseMessage = dto.ResponseMessage?.Trim();
        feedback.ResolvedBy = managerId;
        feedback.ResolvedAt = DateTime.UtcNow;

        // If manager chooses to update OT directly during resolution
        if (dto.UpdateOT && feedback.WorkSchedule != null)
        {
            var schedule = feedback.WorkSchedule;
            if (dto.ApprovedOTHours.HasValue)
            {
                schedule.ApprovedOTHours = dto.ApprovedOTHours.Value;
            }
            if (!string.IsNullOrWhiteSpace(dto.OTStatus))
            {
                schedule.OTStatus = dto.OTStatus;
            }
            if (!string.IsNullOrWhiteSpace(dto.OTNotes))
            {
                schedule.OTNotes = dto.OTNotes;
            }
            schedule.ManagerApproved = true;
            await _workScheduleRepository.UpdateAsync(schedule);
        }

        await _feedbackRepository.UpdateAsync(feedback);
        await _feedbackRepository.SaveChangesAsync();

        var reloaded = await _feedbackRepository.GetByIdAsync(feedback.Id);
        return MapToViewDto(reloaded ?? feedback);
    }

    private static ShiftFeedbackViewDto MapToViewDto(WorkScheduleFeedback entity)
    {
        var ws = entity.WorkSchedule;
        var shift = ws?.Shift;
        var acc = entity.Account;

        return new ShiftFeedbackViewDto
        {
            Id = entity.Id,
            WorkScheduleId = entity.WorkScheduleId,
            AccountId = entity.AccountId,
            EmployeeName = acc?.Name ?? string.Empty,
            EmployeeCode = acc != null ? $"NV{acc.Id:D4}" : string.Empty,
            BranchId = entity.BranchId,
            BranchName = entity.Branch?.Name ?? string.Empty,
            WorkDate = ws?.WorkDate ?? DateOnly.FromDateTime(DateTime.Today),
            ShiftId = ws?.ShiftId ?? 0,
            ShiftName = shift?.Name ?? string.Empty,
            ShiftTime = shift != null ? $"{shift.StartTime:hh\\:mm} - {shift.EndTime:hh\\:mm}" : string.Empty,
            CheckInAt = ws?.CheckInAt,
            CheckOutAt = ws?.CheckOutAt,
            ActualHours = ws?.ActualHours,
            ApprovedOTHours = ws?.ApprovedOTHours ?? 0m,
            OTStatus = ws?.OTStatus ?? "None",
            FeedbackType = entity.FeedbackType,
            Title = entity.Title,
            Content = entity.Content,
            Status = entity.Status.ToString(),
            ResponseMessage = entity.ResponseMessage,
            ResolvedBy = entity.ResolvedBy,
            ResolverName = entity.Resolver?.Name,
            ResolvedAt = entity.ResolvedAt,
            CreatedAt = entity.CreatedAt
        };
    }
}
