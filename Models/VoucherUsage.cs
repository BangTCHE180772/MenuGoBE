using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("VoucherUsages")]
public class VoucherUsage
{
    [Key]
    public long Id { get; set; }

    public long VoucherId { get; set; }

    public long CustomerId { get; set; }

    public long? OrderId { get; set; }

    public DateTime UsedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("VoucherId")]
    public Voucher Voucher { get; set; } = null!;

    [ForeignKey("CustomerId")]
    public Customer Customer { get; set; } = null!;

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }
}
