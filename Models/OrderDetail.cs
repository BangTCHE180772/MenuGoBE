using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("OrderDetails")]
public class OrderDetail
{
    [Key]
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long VariantId { get; set; }

    public long RecipeId { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Price { get; set; }

    public string Note { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string CookingStatus { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;

    [ForeignKey("VariantId")]
    public Variant Variant { get; set; } = null!;

    [ForeignKey("RecipeId")]
    public Recipe Recipe { get; set; } = null!;
}
