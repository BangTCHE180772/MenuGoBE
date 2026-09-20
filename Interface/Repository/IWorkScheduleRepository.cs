using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IWorkScheduleRepository
    {
        Task<List<WorkSchedule>> GetAllAsync();
        Task<WorkSchedule?> GetByIdAsync(long id);
        Task<WorkSchedule?> GetByIdWithDetailsAsync(long id);
        Task CreateAsync(WorkSchedule entity);
        Task UpdateAsync(WorkSchedule entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
        Task<List<WorkSchedule>> GetTodaySchedulesAsync(long accountId, long branchId, DateOnly date);
        Task<bool> ExistsAsync(long accountId, long shiftId, System.DateOnly workDate);
        Task<bool> HasOverlappingScheduleAsync(long accountId, System.DateOnly workDate, TimeOnly startTime, TimeOnly endTime);
        Task<List<WorkSchedule>> GetByAccountIdAsync(long accountId);
        Task<List<WorkSchedule>> GetOpenSchedulesAsync();
        Task<List<WorkSchedule>> GetPendingSchedulesByAccountIdsAsync(List<long> accountIds);
        Task<List<WorkSchedule>> GetExpiredUncheckedInSchedulesAsync();
    }
}
