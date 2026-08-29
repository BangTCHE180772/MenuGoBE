using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Interface.Repository
{
    public interface IShiftChangeRequestRepository
    {
        Task<ShiftChangeRequest?> GetByIdAsync(long id);
        Task<List<ShiftChangeRequest>> GetByBranchAsync(long branchId, ShiftChangeRequestStatus? status);
        Task<List<ShiftChangeRequest>> GetByAccountIdAsync(long accountId);
        Task<bool> HasPendingForScheduleAsync(long workScheduleId);

        Task CreateAsync(ShiftChangeRequest entity);
        Task UpdateAsync(ShiftChangeRequest entity);
        Task SaveChangesAsync();
    }
}
