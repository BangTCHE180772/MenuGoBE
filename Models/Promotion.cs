using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("Promotions")]
public class Promotion
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// "Fixed" hoặc "Percentage"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string DiscountType { get; set; } = "Percentage";

    [Column(TypeName = "decimal(18,4)")]
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// Giá trị giảm tối đa (áp dụng khi DiscountType = Percentage)
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal MaxDiscount { get; set; }

    public PromotionScope Scope { get; set; } = PromotionScope.AllBranches;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("CreatedBy")]
    public Account Creator { get; set; } = null!;

    public ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
    public ICollection<PromotionBranch> PromotionBranches { get; set; } = new List<PromotionBranch>();
}
