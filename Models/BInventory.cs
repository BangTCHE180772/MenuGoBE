using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("BInventories")]
public class BInventory
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    public long ProductId { get; set; }

    public BInventoryType Type { get; set; }

    [Column(TypeName = "decimal(30,12)")]
    public decimal Avg { get; set; }

    [Column(TypeName = "decimal(30,12)")]
    public decimal LeftOver { get; set; }

    [Column(TypeName = "decimal(21,3)")]
    public decimal Quantity { get; set; }

    public bool ChainActive { get; set; }

    public bool BranchActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;

    public ICollection<InventoryLedger> InventoryLedgers { get; set; } = new List<InventoryLedger>();
    public ICollection<DocumentDetail> DocumentDetails { get; set; } = new List<DocumentDetail>();
}
