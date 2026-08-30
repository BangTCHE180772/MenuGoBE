namespace MenuGoBE.Dtos.Order
{
    public class CancelledOrderDetailDto
    {
        public long OrderDetailId { get; set; }
        public long OrderId { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? TableName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        /// <summary>
        /// CookingStatus tại thời điểm huỷ ("Ready" = đơn đã hoàn thành, còn lại = đơn chờ xác nhận).
        /// </summary>
        public string CookingStatusAtCancel { get; set; } = string.Empty;

        /// <summary>
        /// "Completed" (đơn đã hoàn thành — lỗi của bếp) hoặc "Pending" (đơn chờ xác nhận).
        /// </summary>
        public string Category { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
