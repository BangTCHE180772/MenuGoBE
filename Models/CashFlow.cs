using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("CashFlows")]
public class CashFlow
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    public CashFlowDirection Direction { get; set; }

    [Column(TypeName = "decimal(30,12)")]
    public decimal TotalAmount { get; set; }

    public long? DeletedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("DeletedBy")]
    public Account? Deleter { get; set; }

    public ICollection<CashFlowDetail> CashFlowDetails { get; set; } = new List<CashFlowDetail>();
}
