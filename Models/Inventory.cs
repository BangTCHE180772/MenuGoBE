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

    public ICollection<BProductBatch> BProductBatches { get; set; } = new List<BProductBatch>();
    public ICollection<DocumentDetail> DocumentDetails { get; set; } = new List<DocumentDetail>();
}
