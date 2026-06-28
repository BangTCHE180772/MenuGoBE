using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("InventoryTransactionDetailds")]
public class InventoryTransactionDetaild
{
    [Key]
    public long Id { get; set; }

    public long InventoryTransactionBatchId { get; set; }

    public long FromBVariantBatchDetailId { get; set; }

    public long ToBVariantBatchDetailId { get; set; }

    public long UnitConversionId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ClaimQuantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DamageQuantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal WACAtExport { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("InventoryTransactionBatchId")]
    public InventoryTransactionBatch InventoryTransactionBatch { get; set; } = null!;

    [ForeignKey("FromBVariantBatchDetailId")]
    public BVariantBatchDetaild FromBVariantBatchDetail { get; set; } = null!;

    [ForeignKey("ToBVariantBatchDetailId")]
    public BVariantBatchDetaild ToBVariantBatchDetail { get; set; } = null!;

    [ForeignKey("UnitConversionId")]
    public UnitConversion UnitConversion { get; set; } = null!;
}
