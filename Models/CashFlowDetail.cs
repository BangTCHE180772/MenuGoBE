using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("CashFlowDetails")]
public class CashFlowDetail
{
    [Key]
    public long Id { get; set; }

    public long CashFlowId { get; set; }

    public long? DocumentId { get; set; }

    public long? PartnerId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;

    [Column(TypeName = "decimal(27,9)")]
    public decimal Amount { get; set; }

    public string Note { get; set; } = string.Empty;

    public long? DeletedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("CashFlowId")]
    public CashFlow CashFlow { get; set; } = null!;

    [ForeignKey("DocumentId")]
    public Document? Document { get; set; }

    [ForeignKey("PartnerId")]
    public Partner? Partner { get; set; }
}
