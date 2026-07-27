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

    public long? PartnerId { get; set; }

    public CashFlowDetailType Type { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    [Column(TypeName = "decimal(108,12)")]
    public decimal Amount { get; set; }

    public string? Note { get; set; }

    // Navigation
    [ForeignKey("CashFlowId")]
    public CashFlow CashFlow { get; set; } = null!;

    [ForeignKey("DocumentPartnerId")]
    public DocumentPartner? DocumentPartner { get; set; }

    [ForeignKey("PartnerId")]
    public Partner? Partner { get; set; }
}
