namespace MenuGoBE.Dtos.Order
{
    public class OrderItemDto
    {
        public long OrderDetailId { get; set; }
        public long OrderId { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Note { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CookingStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ReturnedQuantity { get; set; }
        public string? ReturnReason { get; set; }
        public string ProductType { get; set; } = string.Empty;
    }
}
