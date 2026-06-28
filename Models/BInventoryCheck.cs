using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("BInventoryCheck")]
public class BInventoryCheck
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    public long InventoryId { get; set; }

    public string Type { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("InventoryId")]
    public Inventory Inventory { get; set; } = null!;

    public ICollection<InventoryCheckDetaild> InventoryCheckDetailds { get; set; } = new List<InventoryCheckDetaild>();
}
