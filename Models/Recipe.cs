using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Recipes")]
public class Recipe
{
    [Key]
    public long Id { get; set; }

    public long VariantId { get; set; }

    [Required]
    public string VersionName { get; set; } = string.Empty;

    public long UnitConversionId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LossRate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("VariantId")]
    public Variant Variant { get; set; } = null!;

    [ForeignKey("UnitConversionId")]
    public UnitConversion UnitConversion { get; set; } = null!;

    public ICollection<RecipesDetailed> RecipesDetaileds { get; set; } = new List<RecipesDetailed>();
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
