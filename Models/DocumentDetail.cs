using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("DocumentDetails")]
public class DocumentDetail
{
    [Key]
    public long Id { get; set; }

    public long DocumentId { get; set; }

    public long BInventoryId { get; set; }

    [Column(TypeName = "decimal(21,3)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(21,3)")]
    public decimal? SystemQuantity { get; set; }

    [Column(TypeName = "decimal(30,12)")]
    public decimal UnitPrice { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("DocumentId")]
    public Document Document { get; set; } = null!;

    [ForeignKey("BInventoryId")]
    public BInventory BInventory { get; set; } = null!;

    public ICollection<InventoryLedger> InventoryLedgers { get; set; } = new List<InventoryLedger>();
}
