using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("PromotionProducts")]
public class PromotionProduct
{
    [Key]
    public long Id { get; set; }

    public long PromotionId { get; set; }

    public long ProductId { get; set; }

    // Navigation
    [ForeignKey("PromotionId")]
    public Promotion Promotion { get; set; } = null!;

    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;
}
