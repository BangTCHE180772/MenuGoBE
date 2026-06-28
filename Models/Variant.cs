using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Variants")]
public class Variant
{
    [Key]
    public long Id { get; set; }

    public long ItemId { get; set; }

    public string Type { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Image { get; set; } = string.Empty;

    [Required]
    public string SKUCode { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool IsSellAble { get; set; }

    public bool IsSecret { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal BasePrice { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal SellPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("ItemId")]
    public Item Item { get; set; } = null!;

    public ICollection<UnitConversion> UnitConversions { get; set; } = new List<UnitConversion>();
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    public ICollection<RecipesDetailed> RecipesDetaileds { get; set; } = new List<RecipesDetailed>();
    public ICollection<BVariant> BVariants { get; set; } = new List<BVariant>();
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
