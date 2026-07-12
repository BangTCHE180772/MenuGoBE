namespace MenuGoBE.Dtos.Order
{
    public class TableOrderSummaryDto
    {
        public long TableId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string TableStatus { get; set; } = string.Empty;
        public string AreaName { get; set; } = string.Empty;
        public long? ActiveOrderId { get; set; }
        public int PendingConfirmCount { get; set; } 
        public int ReadyToServeCount { get; set; }    
        public List<OrderItemDto> Items { get; set; } = new();
    }
}
