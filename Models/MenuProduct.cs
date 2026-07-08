using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("MenuProducts")]
public class MenuProduct
{
    public long MenuId { get; set; }

    public long ProductId { get; set; }

    // Navigation
    [ForeignKey("MenuId")]
    public Menu Menu { get; set; } = null!;

    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;
}
