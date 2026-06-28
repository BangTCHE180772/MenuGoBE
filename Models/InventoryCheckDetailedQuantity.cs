using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("InventoryCheckDetailedQuantities")]
public class InventoryCheckDetailedQuantity
{
    [Key]
    public long Id { get; set; }

    public long InventoryCheckDetailedId { get; set; }

    public long BVariantBatchDetailedId { get; set; }

    public long UnitConversionId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal SystemQuantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ActualQuantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("InventoryCheckDetailedId")]
    public InventoryCheckDetaild InventoryCheckDetaild { get; set; } = null!;

    [ForeignKey("BVariantBatchDetailedId")]
    public BVariantBatchDetaild BVariantBatchDetaild { get; set; } = null!;

    [ForeignKey("UnitConversionId")]
    public UnitConversion UnitConversion { get; set; } = null!;
}
