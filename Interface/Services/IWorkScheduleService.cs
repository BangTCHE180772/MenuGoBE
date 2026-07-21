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
        Task<(bool Success, string Message, string Action)> RecordAttendanceAsync(long accountId, long branchId);
        Task<List<WorkScheduleViewDto>> AssignBulkAsync(WorkScheduleAssignDto dto);
        Task<List<WorkScheduleViewDto>> GetByAccountIdAsync(long accountId);
    }
}
