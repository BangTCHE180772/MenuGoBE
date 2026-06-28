using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("BVariantBatchDetailds")]
public class BVariantBatchDetaild
{
    [Key]
    public long Id { get; set; }

    public long BVariantBatchId { get; set; }

    public long UnitConversionId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BVariantBatchId")]
    public BVariantBatch BVariantBatch { get; set; } = null!;

    [ForeignKey("UnitConversionId")]
    public UnitConversion UnitConversion { get; set; } = null!;
}
