using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.WorkSchedule;

namespace MenuGoBE.Interface.Services
{
    public interface IWorkScheduleService
    {
        Task<List<WorkScheduleViewDto>> GetAllAsync();
        Task<WorkScheduleViewDto?> GetByIdAsync(long id);
        Task<WorkScheduleViewDto> CreateAsync(WorkScheduleCreateDto dto);
        Task<bool> UpdateAsync(WorkScheduleUpdateDto dto);
        Task<bool> DeleteAsync(long id, string reason, long cancelledBy);
        
        // (Check-in/Check-out)
        Task<(bool Success, string Message, string Action)> RecordAttendanceAsync(long accountId, long branchId, long? workScheduleId = null);
        Task<(bool Success, string Message, string Action)> CheckInWithLocationAsync(long accountId, long branchId, double latitude, double longitude, long? workScheduleId = null);
        Task<List<WorkScheduleViewDto>> AssignBulkAsync(WorkScheduleAssignDto dto);
        Task<List<WorkScheduleViewDto>> GetByAccountIdAsync(long accountId);
        Task<List<long>> GetActiveRoleIdsAsync(long accountId);

        Task<bool> RequestLeaveAsync(long accountId, LeaveRequestDto dto);
        Task<bool> RequestShiftSwapAsync(long accountId, ShiftSwapRequestDto dto);
        Task<bool> RejectShiftSwapAsync(long workScheduleId, long senderAccountId, string note, long rejectedBy);

        // OT Approval & Forget Check-out verification
        Task<bool> ApproveOTAsync(WorkScheduleOTApproveDto dto);
        Task<WorkSchedulePOSActivityDto> VerifyPOSActivityAsync(long workScheduleId);

        // Auto Work Schedule Assignment
        Task<WorkScheduleAutoAssignResultDto> AutoAssignAsync(WorkScheduleAutoAssignDto dto);

        Task<List<WorkScheduleActivityLogDto>> GetActivityLogAsync(long workScheduleId);
    }
}
