using System.ComponentModel.DataAnnotations;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Promotion;

public class PromotionUpdateDto
{
    [Required]
    public long Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public string DiscountType { get; set; } = "Percentage";

    public decimal DiscountValue { get; set; }

    public decimal MaxDiscount { get; set; }

    public PromotionScope Scope { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; }

    [Required]
    public List<long> ProductIds { get; set; } = new List<long>();

    public List<long> BranchIds { get; set; } = new List<long>();
}
