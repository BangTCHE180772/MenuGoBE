using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Products")]
public class Product
{
    [Key]
    public long Id { get; set; }

    public long GroupId { get; set; }

    public long? ImageId { get; set; }

    public long ChainId { get; set; }

    public string Type { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string SKUCode { get; set; } = string.Empty;

    public bool IsBatchManage { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public bool IsSecret { get; set; } = false;

    public bool IsSellable { get; set; } = true;

    [Column(TypeName = "decimal(18,0)")]
    public decimal SellPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("GroupId")]
    public Group Group { get; set; } = null!;

    [ForeignKey("ImageId")]
    public Image? Image { get; set; }

    [ForeignKey("ChainId")]
    public Chain Chain { get; set; } = null!;

    public ICollection<MenuProduct> MenuProducts { get; set; } = new List<MenuProduct>();
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    public ICollection<RecipesDetailed> RecipesDetaileds { get; set; } = new List<RecipesDetailed>();
    public ICollection<UnitConversion> UnitConversions { get; set; } = new List<UnitConversion>();
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<BProduct> BProducts { get; set; } = new List<BProduct>();
}
