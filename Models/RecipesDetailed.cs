using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("RecipesDetaileds")]
public class RecipesDetailed
{
    [Key]
    public long Id { get; set; }

    public long ParentProductId { get; set; }

    public long IngredientProductId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("ParentProductId")]
    public Product ParentProduct { get; set; } = null!;

    [ForeignKey("IngredientProductId")]
    public Product IngredientProduct { get; set; } = null!;
}
