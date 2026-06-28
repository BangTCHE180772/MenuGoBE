using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("InventoryTransactionBatches")]
public class InventoryTransactionBatch
{
    [Key]
    public long Id { get; set; }

    public long InventoryTransactionId { get; set; }

    public long FromBVariantBatchId { get; set; }

    public long ToBVariantBatchId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("InventoryTransactionId")]
    public InventoryTransaction InventoryTransaction { get; set; } = null!;

    [ForeignKey("FromBVariantBatchId")]
    public BVariantBatch FromBVariantBatch { get; set; } = null!;

    [ForeignKey("ToBVariantBatchId")]
    public BVariantBatch ToBVariantBatch { get; set; } = null!;

    public ICollection<InventoryTransactionDetaild> InventoryTransactionDetailds { get; set; } = new List<InventoryTransactionDetaild>();
}
