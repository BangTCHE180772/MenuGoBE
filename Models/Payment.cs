using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Models;

[Table("Payments")]
[Index(nameof(OrderId))]
[Index(nameof(Status))]
[Index(nameof(CreatedAt))]
public class Payment
{
    [Key]
    public long Id { get; set; }

    public long OrderId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    public string Method { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? TransactionId { get; set; }

    public string? PaymentLinkId { get; set; }

    public string? CheckoutUrl { get; set; }

    public string? QrCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    // Navigation
    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;
}
