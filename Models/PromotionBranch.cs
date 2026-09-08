using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("PromotionBranches")]
public class PromotionBranch
{
    [Key]
    public long Id { get; set; }

    public long PromotionId { get; set; }

    public long BranchId { get; set; }

    // Navigation
    [ForeignKey("PromotionId")]
    public Promotion Promotion { get; set; } = null!;

    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;
}
