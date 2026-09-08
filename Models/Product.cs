using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("Products")]
public class Product
{
    [Key]
    public long Id { get; set; }

    public long GroupId { get; set; }

    public long? ImageId { get; set; }

    public long ChainId { get; set; }

    public ProductType Type { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string SKUCode { get; set; } = string.Empty;



    public bool IsSellable { get; set; } = true;

    public bool IsManageQuantity { get; set; } = false;

    [Column(TypeName = "decimal(51,3)")]
    public decimal MinStorage { get; set; } = 0;

    [Column(TypeName = "decimal(51,3)")]
    public decimal MaxStorage { get; set; } = 1000000000;

    [Column(TypeName = "decimal(108,12)")]
    public decimal SellPrice { get; set; }

    /// <summary>
    /// Số ngày hạn sử dụng mặc định của sản phẩm / thành phẩm sản xuất (nếu có).
    /// </summary>
    public int? ShelfLifeDays { get; set; }

    /// <summary>
    /// Thời gian chế biến gợi ý (phút) — chủ yếu dùng cho món Chế biến (Processed)
    /// để bếp/khách ước lượng thời gian chờ.
    /// </summary>
    public int? RecommendedTimeMinutes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("GroupId")]
    public Group Group { get; set; } = null!;

    [ForeignKey("ImageId")]
    public Image? Image { get; set; }

    [ForeignKey("ChainId")]
    public Chain Chain { get; set; } = null!;

    public ICollection<MenuProduct> MenuProducts { get; set; } = new List<MenuProduct>();
    public ICollection<RecipesDetailed> RecipeItems { get; set; } = new List<RecipesDetailed>();
    public ICollection<RecipesDetailed> UsedInRecipeItems { get; set; } = new List<RecipesDetailed>();
    public ICollection<UnitConversion> UnitConversions { get; set; } = new List<UnitConversion>();
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<BInventory> BInventories { get; set; } = new List<BInventory>();
}
