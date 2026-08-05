namespace MenuGoBE.Dtos.Product
{
    public class ToolProductCreateDto
    {
        public long GroupId { get; set; }
        public long? ImageId { get; set; }
        public string? ImageUrl { get; set; }
        public long ChainId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SKUCode { get; set; } = string.Empty;
        public bool IsManageQuantity { get; set; } = false;
        public decimal MinStorage { get; set; } = 0;
        public decimal MaxStorage { get; set; } = 1000000000;
        public List<ProductUnitConversionDto>? UnitConversions { get; set; }
    }

    public class ToolProductUpdateDto : ToolProductCreateDto
    {
        public long Id { get; set; }
    }
}
