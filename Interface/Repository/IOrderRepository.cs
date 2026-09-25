using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IOrderRepository
    {
        Task<List<Order>> GetAllAsync();
        Task<Order?> GetByIdAsync(long id);
        Task<Order?> GetOrderByIdWithDetailsAsync(long orderId);
        Task<List<Order>> GetChildOrdersAsync(long fatherId);
        Task<List<Order>> GetChildOrdersWithDetailsAsync(long fatherId);
        Task<Order?> GetActiveOrderByTableIdAsync(long tableId);
        Task<Order?> GetActiveOrderByIdWithDetailsAsync(long orderId);
        Task<List<Order>> GetActiveKitchenOrdersWithDetailsAsync(long? branchId = null);
        Task<List<Order>> GetPaidOrdersByBranchAsync(long branchId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<Order>> GetReturnableOrdersByBranchAsync(long branchId);

        Task CreateAsync(Order entity);
        Task UpdateAsync(Order entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
        Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync();
        Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy CreateExecutionStrategy();
        void ClearChangeTracker();
    }
}
