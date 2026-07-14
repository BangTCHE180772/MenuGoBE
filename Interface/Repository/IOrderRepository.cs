using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IOrderRepository
    {
        Task<List<Order>> GetAllAsync();
        Task<Order?> GetByIdAsync(long id);
        Task<List<Order>> GetChildOrdersAsync(long fatherId);
        Task<List<Order>> GetChildOrdersWithDetailsAsync(long fatherId);
        Task<Order?> GetActiveOrderByTableIdAsync(long tableId);
        Task<Order?> GetActiveOrderByIdWithDetailsAsync(long orderId);

        Task CreateAsync(Order entity);
        Task UpdateAsync(Order entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
    }
}
