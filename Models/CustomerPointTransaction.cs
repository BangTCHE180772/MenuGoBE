using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("CustomerPointTransactions")]
public class CustomerPointTransaction
{
    [Key]
    public long Id { get; set; }

    public long CustomerId { get; set; }

    public long? OrderId { get; set; }

    [MaxLength(50)]
    public string? RelatedCode { get; set; }

    public PointTransactionType Type { get; set; }

    public int PointChange { get; set; }

    public int PointBefore { get; set; }

    public int PointAfter { get; set; }

    [MaxLength(255)]
    public string Reason { get; set; } = string.Empty;

    public bool IsManual { get; set; }

    public bool IsReversed { get; set; } = false;

    public long? ReversedTransactionId { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("CustomerId")]
    public Customer Customer { get; set; } = null!;

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    [ForeignKey("CreatedBy")]
    public Account? Creator { get; set; }
}
