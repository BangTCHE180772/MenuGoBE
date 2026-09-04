namespace MenuGoBE.Dtos.Order
{
    public class OrderDetailCreateDto
    {
        public long OrderId { get; set; }

        public long ProductId { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public string Note { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string CookingStatus { get; set; } = string.Empty;

        // Gợi ý dùng lại món thừa: Id của LeftoverRecord (Return, Processed, còn trong
        // 30p) mà món này thay thế thay vì bắt bếp làm mới.
        public long? ReuseLeftoverId { get; set; }
    }
}
