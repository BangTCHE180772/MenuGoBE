using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("RecipesDetaileds")]
public class RecipesDetailed
{
    [Key]
    public long Id { get; set; }

    public long ProductId { get; set; }

    public long RecipeId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;

    [ForeignKey("RecipeId")]
    public Recipe Recipe { get; set; } = null!;
}
