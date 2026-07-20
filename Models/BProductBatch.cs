using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("BProductBatches")]
public class BProductBatch
{
    [Key]
    public long Id { get; set; }

    /// <summary>FK → BProducts (originally named BVariantId in schema — corrected to BProductId)</summary>
    public long BProductId { get; set; }

    public long InventoryId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public DateOnly? ExpiryDate { get; set; }

    [Column(TypeName = "decimal(21,3)")]
    public decimal Quantity { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BProductId")]
    public BProduct BProduct { get; set; } = null!;

    [ForeignKey("InventoryId")]
    public Inventory Inventory { get; set; } = null!;

    public ICollection<BBatchesHistory> BBatchesHistories { get; set; } = new List<BBatchesHistory>();
    public ICollection<DocumentDetail> DocumentDetails { get; set; } = new List<DocumentDetail>();
}
