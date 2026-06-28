using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("InventoryCheckDetailds")]
public class InventoryCheckDetaild
{
    [Key]
    public long Id { get; set; }

    public long InventoryCheckId { get; set; }

    public long BVariantBatchId { get; set; }

    public string SSName { get; set; } = string.Empty;

    public string SSImage { get; set; } = string.Empty;

    public string SSSKUCode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Cost { get; set; }

    public string Note { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("InventoryCheckId")]
    public BInventoryCheck BInventoryCheck { get; set; } = null!;

    [ForeignKey("BVariantBatchId")]
    public BVariantBatch BVariantBatch { get; set; } = null!;

    public ICollection<InventoryCheckDetailedQuantity> InventoryCheckDetailedQuantities { get; set; } = new List<InventoryCheckDetailedQuantity>();
}
