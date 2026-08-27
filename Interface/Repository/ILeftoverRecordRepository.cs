using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Interface.Repository
{
    public interface ILeftoverRecordRepository
    {
        Task<List<LeftoverRecord>> GetByBranchAsync(long branchId, LeftoverType? type, DateOnly? fromDate, DateOnly? toDate);
        Task<LeftoverRecord?> GetByIdAsync(long id);
        Task<OrderDetail?> GetOrderDetailWithTableAsync(long orderDetailId);

        Task CreateAsync(LeftoverRecord entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
    }
}
