using System.Text.Json.Serialization;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Product
{
    public class ProductViewDto
    {
        public long Id { get; set; }
        public long? GroupId { get; set; }
        public long? ImageId { get; set; }
        public string? ImageUrl { get; set; }
        public long ChainId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProductType Type { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SKUCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsSellable { get; set; }
        public decimal SellPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ProductUnitConversionViewDto>? UnitConversions { get; set; }
        public List<ProductRecipeDetailedViewDto>? RecipeItems { get; set; }
    }
}
