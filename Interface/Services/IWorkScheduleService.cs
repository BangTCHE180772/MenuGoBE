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
        Task<List<WorkScheduleViewDto>> AssignBulkAsync(WorkScheduleAssignDto dto);
    }
}
