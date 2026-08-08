namespace MenuGoBE.Dtos.Order
{
    public class TableOrderSummaryDto
    {
        public long TableId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string TableStatus { get; set; } = string.Empty;
        public string AreaName { get; set; } = string.Empty;
        public long BranchId { get; set; }
        public long? ActiveOrderId { get; set; }
        public long? OriginalOrderId { get; set; }
        public bool IsMerged { get; set; }
        public int PendingConfirmCount { get; set; } 
        public int ReadyToServeCount { get; set; }    

        public long? ReservationId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public DateTime? ReservationTime { get; set; }

        public List<OrderItemDto> Items { get; set; } = new();
    }
}
