using MenuGoBE.Dtos.Order;

namespace MenuGoBE.Interface.Services
{
    public interface IOrderService
    {
        Task<List<OrderViewDto>> GetAllAsync();
        Task<OrderViewDto?> GetByIdAsync(long id);

        Task<OrderViewDto> CreateAsync(OrderCreateDto dto);
        Task<bool> UpdateAsync(OrderUpdateDto dto);
        Task<bool> DeleteAsync(long id);

        Task<bool> MergeAsync(OrderMergeDto dto);
        Task<bool> UnmergeAsync(long childOrderId);
        Task<bool> MoveTableAsync(long orderId, long newTableId);
        Task<List<OrderViewDto>> GetChildOrdersAsync(long fatherOrderId);
        Task<OrderMergedSummaryDto?> GetMergedSummaryAsync(long fatherOrderId);

        Task<TableOrderSummaryDto> GetTableOrderSummaryAsync(long tableId);
        Task<List<long>> PayOrderAsync(long orderId, OrderPayDto? dto = null, string paymentMethod = "Cash", long? userId = null);
        Task<decimal> PreparePaymentAsync(long orderId, OrderPayDto? dto, string paymentMethod = "Cash");
        Task<bool> CancelOrderAsync(long orderId);
        Task<List<OrderViewDto>> GetPaidOrdersByBranchAsync(long branchId);
        Task<List<OrderViewDto>> GetReturnableOrdersByBranchAsync(long branchId);
    }
}
