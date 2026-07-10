namespace MenuGoBE.Dtos.Product
{
    public class ProductRecipeViewDto
    {
        public long Id { get; set; }
        public string VersionName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ProductRecipeDetailedViewDto> Details { get; set; } = new();
    }
}
