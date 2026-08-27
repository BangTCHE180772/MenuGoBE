namespace MenuGoBE.Dtos.Order
{
    public class KitchenItemDto
    {
        public long OrderDetailId { get; set; }
        public long OrderId { get; set; }
        public long TableId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Note { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CookingStatus { get; set; } = string.Empty;
        public string? BatchId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
