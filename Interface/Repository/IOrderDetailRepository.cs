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
        Task SaveChangesAsync();
    }
}
