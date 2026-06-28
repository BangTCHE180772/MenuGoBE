using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("SuppliersTransactions")]
public class SuppliersTransaction
{
    [Key]
    public long Id { get; set; }

    public long ToBInventoryId { get; set; }

    public long TransportId { get; set; }

    public long SupplierId { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,4)")]
    public decimal GoodsCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TransportCost { get; set; }

    public string SSName { get; set; } = string.Empty;

    public string SSPhone { get; set; } = string.Empty;

    public string SSLisencePlate { get; set; } = string.Empty;

    public string SSEmail { get; set; } = string.Empty;

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("ToBInventoryId")]
    public Inventory Inventory { get; set; } = null!;

    [ForeignKey("TransportId")]
    public Transport Transport { get; set; } = null!;

    [ForeignKey("SupplierId")]
    public Supplier Supplier { get; set; } = null!;

    public ICollection<BInInventoryTransactionDetaild> BInInventoryTransactionDetailds { get; set; } = new List<BInInventoryTransactionDetaild>();
}
