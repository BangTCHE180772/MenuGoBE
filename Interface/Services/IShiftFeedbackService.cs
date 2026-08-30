using MenuGoBE.Dtos.WorkSchedule;

namespace MenuGoBE.Interface.Services;

public interface IShiftFeedbackService
{
    Task<ShiftFeedbackViewDto> CreateFeedbackAsync(long accountId, ShiftFeedbackCreateDto dto);
    Task<List<ShiftFeedbackViewDto>> GetMyFeedbacksAsync(long accountId);
    Task<List<ShiftFeedbackViewDto>> GetBranchFeedbacksAsync(long branchId, string? statusStr);
    Task<ShiftFeedbackViewDto> ProcessFeedbackAsync(long feedbackId, long managerId, ShiftFeedbackProcessDto dto);
}
