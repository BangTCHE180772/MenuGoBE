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
        Task<bool> DeleteAsync(long id);
        
        // (Check-in/Check-out)
        Task<(bool Success, string Message, string Action)> RecordAttendanceAsync(long accountId, long branchId, long? workScheduleId = null);
        Task<(bool Success, string Message, string Action)> CheckInWithLocationAsync(long accountId, long branchId, double latitude, double longitude, long? workScheduleId = null);
        Task<List<WorkScheduleViewDto>> AssignBulkAsync(WorkScheduleAssignDto dto);
        Task<List<WorkScheduleViewDto>> GetByAccountIdAsync(long accountId);
        Task<List<long>> GetActiveRoleIdsAsync(long accountId);

        Task<bool> RequestLeaveAsync(long accountId, LeaveRequestDto dto);
        Task<bool> RequestShiftSwapAsync(long accountId, ShiftSwapRequestDto dto);

        // OT Approval & Forget Check-out verification
        Task<bool> ApproveOTAsync(WorkScheduleOTApproveDto dto);
        Task<WorkSchedulePOSActivityDto> VerifyPOSActivityAsync(long workScheduleId);
    }
}
