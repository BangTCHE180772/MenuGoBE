namespace MenuGoBE.Dtos.Product
{
    public class ProductUnitConversionViewDto
    {
        public long Id { get; set; }
        public long UnitId { get; set; }
        public string? UnitName { get; set; }
        public bool IsBase { get; set; }
        public long? BaseId { get; set; }
        public decimal ConversionPoint { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
