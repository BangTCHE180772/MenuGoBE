using MenuGoBE.Dtos.Order;

namespace MenuGoBE.Interface.Services
{
    public interface IOrderDetailService
    {
        Task<List<OrderDetailViewDto>> GetByOrderIdAsync(long orderId);
        Task<OrderDetailViewDto?> GetByIdAsync(long id);
        Task<int> GetAvailableQuantityAsync(long productId, long branchId, long? excludeOrderDetailId = null);
        Task<Dictionary<long, int>> GetAvailableQuantitiesAsync(long branchId, List<long> productIds);
        Task<Dictionary<long, int>> GetBulkMenuAvailableQuantitiesAsync(long branchId);
        Task<OrderDetailViewDto> CreateAsync(OrderDetailCreateDto dto);
        Task<BulkOrderResultDto> CreateMultipleAsync(List<OrderDetailCreateDto> dtos);
        Task<bool> UpdateAsync(OrderDetailUpdateDto dto);
        Task<bool> DeleteAsync(long id);

        Task<bool> ConfirmAsync(long orderDetailId, int? finalQuantity = null);
        Task<bool> RejectAsync(long orderDetailId, string reason);
        Task<bool> UpdateCookingStatusAsync(long orderDetailId, string cookingStatus, int? quantity = null);
        Task<bool> ApplyLeftoverReuseAsync(long orderDetailId, long leftoverId);
        Task<BatchCookingResultDto> BatchUpdateCookingStatusAsync(long productId, string cookingStatus, long[] branchIds);
        Task<bool> CancelOrderDetailAsync(long orderDetailId, long? cancelledBy = null);
        Task<List<KitchenItemDto>> GetKitchenItemsAsync(long? branchId = null);
        Task<List<CancelledOrderDetailDto>> GetCancelledHistoryAsync(long branchId, DateTime? fromDate, DateTime? toDate);

        Task<bool> RequestReturnItemAsync(ReturnItemRequestDto dto, long confirmedByUserId);
        Task<bool> ConfirmReturnItemAsync(long orderDetailId, long confirmedByUserId);
    }
}
