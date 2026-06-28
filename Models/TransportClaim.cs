using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("TransportClaims")]
public class TransportClaim
{
    [Key]
    public long Id { get; set; }

    public long InventoryTransactionId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalClaim { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal SettledClaim { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public DateTime? ResolvedAt { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("InventoryTransactionId")]
    public InventoryTransaction InventoryTransaction { get; set; } = null!;

    public ICollection<Evidence> Evidences { get; set; } = new List<Evidence>();
}
