using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Order
{
    public class BatchUpdateCookingStatusDto
    {
        [Required]
        public long ProductId { get; set; }

        [Required]
        public string CookingStatus { get; set; } = string.Empty;

        public long? BranchId { get; set; }

        public int? Quantity { get; set; }
    }
}
