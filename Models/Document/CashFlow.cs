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

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long PostingSequence { get; set; }

    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    public DateTime BusinessDate { get; set; }

    public CashFlowDirection Direction { get; set; }

    public CashFlowStatus Status { get; set; }

    [Column(TypeName = "decimal(108,12)")]
    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;

    public long? DeletedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("DeletedBy")]
    public Account? Deleter { get; set; }

    public ICollection<CashFlowDetail> CashFlowDetails { get; set; } = new List<CashFlowDetail>();
}
