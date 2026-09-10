namespace MenuGoBE.Dtos.Product
{
    public class ProcessedProductCreateDto
    {
        public long GroupId { get; set; }
        public long? ImageId { get; set; }
        public string? ImageUrl { get; set; }
        public long ChainId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SKUCode { get; set; } = string.Empty;
        public bool IsSellable { get; set; } = true;
        public decimal SellPrice { get; set; }
        public int? RecommendedTimeMinutes { get; set; }
        public List<ProductRecipeDetailedDto>? Recipe { get; set; }
        public List<ProductUnitConversionDto>? UnitConversions { get; set; }
        public List<long>? MenuIds { get; set; }
    }

    public class ProcessedProductUpdateDto : ProcessedProductCreateDto
    {
        public long Id { get; set; }
    }
}
