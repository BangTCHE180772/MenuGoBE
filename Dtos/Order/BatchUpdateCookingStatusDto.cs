using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Order
{
    public class BatchUpdateCookingStatusDto
    {
        [Required]
        public long ProductId { get; set; }

        [Required]
        public string CookingStatus { get; set; } = string.Empty;
    }
}
