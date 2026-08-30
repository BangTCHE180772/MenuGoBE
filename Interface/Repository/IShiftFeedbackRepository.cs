using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Interface.Repository;

public interface IShiftFeedbackRepository
{
    Task<WorkScheduleFeedback?> GetByIdAsync(long id);
    Task<List<WorkScheduleFeedback>> GetByAccountIdAsync(long accountId);
    Task<List<WorkScheduleFeedback>> GetByBranchAsync(long branchId, ShiftFeedbackStatus? status);
    Task CreateAsync(WorkScheduleFeedback entity);
    Task UpdateAsync(WorkScheduleFeedback entity);
    Task SaveChangesAsync();
}
