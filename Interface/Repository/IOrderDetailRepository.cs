using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IOrderDetailRepository
    {
        Task<List<OrderDetail>> GetByOrderIdAsync(long orderId);
        Task<OrderDetail?> GetByIdAsync(long id);

        Task CreateAsync(OrderDetail entity);
        Task UpdateAsync(OrderDetail entity);
        Task DeleteAsync(long id);
        Task<bool> UpdateStatusAsync(long id, string status);
        Task<bool> UpdateCookingStatusAsync(long id, string cookingStatus);
        Task<int> GetPendingQuantityAsync(long productId, long branchId);
        Task<List<OrderDetail>> GetPendingByProductIdAsync(long productId, long[] branchIds);
        Task<List<OrderDetail>> GetCancelledByBranchAsync(long branchId, DateTime? fromDate, DateTime? toDate);
        Task<(Dictionary<long, int> direct, Dictionary<long, int> ingredient)> GetBulkPendingQuantitiesAsync(long branchId);
        Task SaveChangesAsync();
    }
}
