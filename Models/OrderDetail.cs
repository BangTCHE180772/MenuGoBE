using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Models;

[Table("OrderDetails")]
[Index(nameof(OrderId))]
[Index(nameof(ProductId))]
[Index(nameof(Status))]
[Index(nameof(CookingStatus))]
public class OrderDetail
{
    [Key]
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long ProductId { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Price { get; set; }

    public string Note { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string CookingStatus { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? BatchId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;

    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Promotion đã áp dụng cho OrderDetail này (null = không có KM)
    /// </summary>
    public long? PromotionId { get; set; }

    /// <summary>
    /// Giá gốc trước khi áp dụng khuyến mãi
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal OriginalPrice { get; set; }

    [ForeignKey("PromotionId")]
    public Promotion? Promotion { get; set; }

    public int ReturnedQuantity { get; set; } = 0;

    [MaxLength(255)]
    public string? ReturnReason { get; set; }

    public bool ReturnIsIntact { get; set; } = false;

    public DateTime? ReturnedAt { get; set; }
    public long? ReturnConfirmedBy { get; set; }
}
