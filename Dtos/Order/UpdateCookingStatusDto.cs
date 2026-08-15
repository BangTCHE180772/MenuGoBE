using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Order
{
    public class UpdateCookingStatusDto
    {
        [Required]
        public string CookingStatus { get; set; } = string.Empty;
        public int? Quantity { get; set; }
    }
}
