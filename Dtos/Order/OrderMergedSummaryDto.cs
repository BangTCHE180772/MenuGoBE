namespace MenuGoBE.Dtos.Order
{
    public class OrderMergedSummaryDto
    {
        public OrderViewDto FatherOrder { get; set; } = null!;

        public List<OrderViewDto> ChildOrders { get; set; } = new();

        // Tổng thanh toán = FatherOrder.TotalAmount + Σ ChildOrders.TotalAmount
        public decimal GrandTotal { get; set; }
    }
}
