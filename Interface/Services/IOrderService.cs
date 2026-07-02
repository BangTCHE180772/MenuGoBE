using MenuGoBE.Dtos.Order;

namespace MenuGoBE.Interface.Services
{
    public interface IOrderService
    {
        // Order
        Task<List<OrderViewDto>> GetAllAsync();
        Task<OrderViewDto?> GetByIdAsync(long id);
        Task<OrderViewDto> CreateAsync(OrderCreateDto dto);
        Task<bool> UpdateAsync(OrderUpdateDto dto);
        Task<bool> DeleteAsync(long id);

        // OrderDetail - gộp nghiệp vụ vào cùng service
        Task<List<OrderDetailViewDto>> GetDetailsByOrderIdAsync(long orderId);
        Task<OrderDetailViewDto?> GetDetailByIdAsync(long id);
        Task<OrderDetailViewDto> CreateDetailAsync(OrderDetailCreateDto dto);
        Task<bool> UpdateDetailAsync(OrderDetailUpdateDto dto);
        Task<bool> DeleteDetailAsync(long id);
    }
}
