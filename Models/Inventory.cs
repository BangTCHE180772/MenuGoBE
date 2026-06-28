using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Inventories")]
public class Inventory
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    public ICollection<BVariantBatch> BVariantBatches { get; set; } = new List<BVariantBatch>();
    public ICollection<SuppliersTransaction> SuppliersTransactions { get; set; } = new List<SuppliersTransaction>();

    [InverseProperty("FromInventory")]
    public ICollection<InventoryTransaction> FromTransactions { get; set; } = new List<InventoryTransaction>();

    [InverseProperty("ToInventory")]
    public ICollection<InventoryTransaction> ToTransactions { get; set; } = new List<InventoryTransaction>();

    public ICollection<BInventoryCheck> BInventoryChecks { get; set; } = new List<BInventoryCheck>();
}
