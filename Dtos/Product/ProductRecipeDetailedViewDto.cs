namespace MenuGoBE.Dtos.Product
{
    public class ProductRecipeDetailedViewDto
    {
        public long Id { get; set; }
        public long IngredientProductId { get; set; }
        public string? IngredientProductName { get; set; }
        public string? IngredientSKUCode { get; set; }
        public decimal Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
