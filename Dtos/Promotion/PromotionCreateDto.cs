using System.ComponentModel.DataAnnotations;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Promotion;

public class PromotionCreateDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public string DiscountType { get; set; } = "Percentage"; // "Fixed" or "Percentage"

    public decimal DiscountValue { get; set; }

    public decimal MaxDiscount { get; set; }

    public PromotionScope Scope { get; set; } = PromotionScope.AllBranches;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    public List<long> ProductIds { get; set; } = new List<long>();

    // Nếu Scope = SpecificBranches, mảng này bắt buộc có dữ liệu
    public List<long> BranchIds { get; set; } = new List<long>();
}
