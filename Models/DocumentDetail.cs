using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("DocumentDetails")]
public class DocumentDetail
{
    [Key]
    public long Id { get; set; }

    public long DocumentId { get; set; }

    public long BProductId { get; set; }

    public long BProductBatchId { get; set; }

    public long InventoryId { get; set; }

    [Column(TypeName = "decimal(21,3)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(27,9)")]
    public decimal UnitPrice { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public string Note { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("DocumentId")]
    public Document Document { get; set; } = null!;

    [ForeignKey("BProductId")]
    public BProduct BProduct { get; set; } = null!;

    [ForeignKey("BProductBatchId")]
    public BProductBatch BProductBatch { get; set; } = null!;

    [ForeignKey("InventoryId")]
    public Inventory Inventory { get; set; } = null!;

    public ICollection<BBatchesHistory> BBatchesHistories { get; set; } = new List<BBatchesHistory>();
}
