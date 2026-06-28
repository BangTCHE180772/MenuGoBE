using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Transports")]
public class Transport
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string LisencePlate { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    public ICollection<SuppliersTransaction> SuppliersTransactions { get; set; } = new List<SuppliersTransaction>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
