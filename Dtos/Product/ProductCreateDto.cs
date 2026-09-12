using System.Text.Json.Serialization;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Product
{
    public class ProductCreateDto
    {
        public long GroupId { get; set; }
        public long? ImageId { get; set; }
        public string? ImageUrl { get; set; }
        public long ChainId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProductType? Type { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SKUCode { get; set; } = string.Empty;
        public bool IsSellable { get; set; } = true;
        public bool IsManageQuantity { get; set; } = false;
        public decimal MinStorage { get; set; } = 0;
        public decimal MaxStorage { get; set; } = 1000000000;
        public decimal SellPrice { get; set; }
        public int? RecommendedTimeMinutes { get; set; }
        public List<ProductUnitConversionDto>? UnitConversions { get; set; }
        public List<ProductRecipeDetailedDto>? Recipe { get; set; }
        public List<long>? MenuIds { get; set; }
    }
}
