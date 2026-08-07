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

        Task<bool> ConfirmAsync(long orderDetailId);
        Task<bool> UpdateCookingStatusAsync(long orderDetailId, string cookingStatus);
        Task<bool> CancelOrderDetailAsync(long orderDetailId);
        Task<List<KitchenItemDto>> GetKitchenItemsAsync();

        Task<bool> RequestReturnItemAsync(ReturnItemRequestDto dto, long confirmedByUserId);
        Task<bool> ConfirmReturnItemAsync(long orderDetailId, long confirmedByUserId);
    }
}
