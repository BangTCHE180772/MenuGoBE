using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("BVariantBatches")]
public class BVariantBatch
{
    [Key]
    public long Id { get; set; }

    public long BVariantId { get; set; }

    public long InventoryId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime? ExpireDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BVariantId")]
    public BVariant BVariant { get; set; } = null!;

    [ForeignKey("InventoryId")]
    public Inventory Inventory { get; set; } = null!;

    public ICollection<BVariantBatchDetaild> BVariantBatchDetailds { get; set; } = new List<BVariantBatchDetaild>();
}
