using MenuGoBE.Dtos.WorkSchedule;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Interface.Services
{
    public interface IShiftChangeRequestService
    {
        Task<ShiftChangeRequestViewDto> CreateAsync(long accountId, ShiftChangeRequestCreateDto dto);
        Task<List<ShiftChangeRequestViewDto>> GetByBranchAsync(long branchId, ShiftChangeRequestStatus? status);
        Task<List<ShiftChangeRequestViewDto>> GetMineAsync(long accountId);
        Task<bool> ApproveAsync(long requestId, long approverId);
        Task<bool> RejectAsync(long requestId, long approverId, string? reason);
    }
}
