using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("RecipesDetaileds")]
public class RecipesDetailed
{
    [Key]
    public long Id { get; set; }

    public long VariantId { get; set; }

    public long RecipeId { get; set; }

    public long UnitConversionId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LossRate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("VariantId")]
    public Variant Variant { get; set; } = null!;

    [ForeignKey("RecipeId")]
    public Recipe Recipe { get; set; } = null!;

    [ForeignKey("UnitConversionId")]
    public UnitConversion UnitConversion { get; set; } = null!;
}
