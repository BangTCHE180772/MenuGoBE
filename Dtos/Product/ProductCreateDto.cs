namespace MenuGoBE.Dtos.Product
{
    public class ProductCreateDto
    {
        public long GroupId { get; set; }
        public long? ImageId { get; set; }
        public long ChainId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SKUCode { get; set; } = string.Empty;
        public bool IsBatchManage { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSecret { get; set; }
        public bool IsSellable { get; set; } = true;
        public decimal SellPrice { get; set; }
        public List<ProductUnitConversionDto>? UnitConversions { get; set; }
        public ProductRecipeDto? Recipe { get; set; }
    }
}
