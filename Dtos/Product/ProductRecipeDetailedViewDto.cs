namespace MenuGoBE.Dtos.Product
{
    public class ProductRecipeDetailedViewDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public decimal Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
