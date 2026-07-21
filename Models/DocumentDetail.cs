using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("DocumentDetails")]
public class DocumentDetail
{
    [Key]
    public long Id { get; set; }

    public long DocumentId { get; set; }

    public long BProductId { get; set; }

    public long? RecipeId { get; set; }

    public bool IsLeftOver { get; set; }

    [Column(TypeName = "decimal(21,3)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(30,12)")]
    public decimal UnitPrice { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("DocumentId")]
    public Document Document { get; set; } = null!;

    [ForeignKey("BProductId")]
    public BProduct BProduct { get; set; } = null!;

    [ForeignKey("RecipeId")]
    public Recipe? Recipe { get; set; }

    public ICollection<BProductHistory> BProductHistories { get; set; } = new List<BProductHistory>();
}
