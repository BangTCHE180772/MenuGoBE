using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("InventoryTransactions")]
public class InventoryTransaction
{
    [Key]
    public long Id { get; set; }

    public long FromInventoryId { get; set; }

    public long ToInventoryId { get; set; }

    public long TransportId { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,4)")]
    public decimal GoodsCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TransportCost { get; set; }

    public string SSLisencePlate { get; set; } = string.Empty;

    public string SSPhone { get; set; } = string.Empty;

    public string SSName { get; set; } = string.Empty;

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("FromInventoryId")]
    public Inventory FromInventory { get; set; } = null!;

    [ForeignKey("ToInventoryId")]
    public Inventory ToInventory { get; set; } = null!;

    [ForeignKey("TransportId")]
    public Transport Transport { get; set; } = null!;

    public ICollection<InventoryTransactionBatch> InventoryTransactionBatches { get; set; } = new List<InventoryTransactionBatch>();
    public ICollection<TransportClaim> TransportClaims { get; set; } = new List<TransportClaim>();
}
