namespace MenuGoBE.Dtos.Product
{
    public class ProductRecipeDto
    {
        public string VersionName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public List<ProductRecipeDetailedDto> Details { get; set; } = new();
    }
}
