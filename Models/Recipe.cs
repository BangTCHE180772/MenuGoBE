using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Recipes")]
public class Recipe
{
    [Key]
    public long Id { get; set; }

    public long ProductId { get; set; }

    public string VersionName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;

    public ICollection<RecipesDetailed> RecipesDetaileds { get; set; } = new List<RecipesDetailed>();
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<DocumentDetail> DocumentDetails { get; set; } = new List<DocumentDetail>();
}
