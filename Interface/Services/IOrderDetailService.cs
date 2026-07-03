using MenuGoBE.Dtos.Order;

namespace MenuGoBE.Interface.Services
{
    public interface IOrderDetailService
    {
        Task<List<OrderDetailViewDto>> GetByOrderIdAsync(long orderId);
        Task<OrderDetailViewDto?> GetByIdAsync(long id);

        Task<OrderDetailViewDto> CreateAsync(OrderDetailCreateDto dto);
        Task<bool> UpdateAsync(OrderDetailUpdateDto dto);
        Task<bool> DeleteAsync(long id);
    }
}
