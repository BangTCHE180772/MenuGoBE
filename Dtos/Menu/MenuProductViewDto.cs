using System.Text.Json.Serialization;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Menu
{
    public class MenuProductViewDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProductType Type { get; set; }

        public decimal SellPrice { get; set; }
        public bool IsSellable { get; set; }


        public long? GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string ImageLink { get; set; } = string.Empty;
    }
}
