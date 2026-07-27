namespace MenuGoBE.Dtos.Order
{
    public class OrderDetailUpdateDto
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public long ProductId { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public string Note { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string CookingStatus { get; set; } = string.Empty;
    }
}
