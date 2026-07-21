using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("CashFlowDetails")]
public class CashFlowDetail
{
    [Key]
    public long Id { get; set; }

    public long CashFlowId { get; set; }

    public long? DocumentPartnerId { get; set; }

    public CashFlowDetailType Type { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    [Column(TypeName = "decimal(30,12)")]
    public decimal Amount { get; set; }

    public string? Note { get; set; }

    public long? DeletedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("CashFlowId")]
    public CashFlow CashFlow { get; set; } = null!;

    [ForeignKey("DocumentPartnerId")]
    public DocumentPartner? DocumentPartner { get; set; }

    [ForeignKey("DeletedBy")]
    public Account? Deleter { get; set; }
}
