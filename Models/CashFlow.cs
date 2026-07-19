using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("CashFlows")]
public class CashFlow
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    /// <summary>Unique business code for this cash flow record</summary>
    public string Code { get; set; } = string.Empty;

    public string Direction { get; set; } = string.Empty;

    [Column(TypeName = "decimal(27,9)")]
    public decimal TotalAmount { get; set; }

    public long? DeletedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    public ICollection<CashFlowDetail> CashFlowDetails { get; set; } = new List<CashFlowDetail>();
}
