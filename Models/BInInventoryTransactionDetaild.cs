using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("BInInventoryTransactionDetailds")]
public class BInInventoryTransactionDetaild
{
    [Key]
    public long Id { get; set; }

    public long SuppliersTransactionId { get; set; }

    public long VariantId { get; set; }

    public long BVariantBatchId { get; set; }

    public long UnitConversionId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalCost { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("SuppliersTransactionId")]
    public SuppliersTransaction SuppliersTransaction { get; set; } = null!;

    [ForeignKey("VariantId")]
    public Variant Variant { get; set; } = null!;

    [ForeignKey("BVariantBatchId")]
    public BVariantBatch BVariantBatch { get; set; } = null!;

    [ForeignKey("UnitConversionId")]
    public UnitConversion UnitConversion { get; set; } = null!;
}
