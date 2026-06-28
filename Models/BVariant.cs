using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("BVariants")]
public class BVariant
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    public long VariantId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal PricePerBaseUnit { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalCost { get; set; }

    public bool ChainActive { get; set; }

    public bool BranchActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("VariantId")]
    public Variant Variant { get; set; } = null!;

    public ICollection<BVariantBatch> BVariantBatches { get; set; } = new List<BVariantBatch>();
}
