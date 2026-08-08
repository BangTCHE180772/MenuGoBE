using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("CashFlows")]
[Index(nameof(Code), IsUnique = true)]
public class CashFlow
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    public long? DocumentId { get; set; }

    public long? PartnerId { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long PostingSequence { get; set; }

    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    public DateTime BusinessDate { get; set; }

    public CashFlowDirection Direction { get; set; }

    public CashFlowDetailType Type { get; set; } = CashFlowDetailType.Payment;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public CashFlowStatus Status { get; set; }

    [Column(TypeName = "decimal(108,12)")]
    public decimal TotalAmount { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;

    public long? DeletedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("DocumentId")]
    public Document? Document { get; set; }

    [ForeignKey("PartnerId")]
    public Partner? Partner { get; set; }

    [ForeignKey("DeletedBy")]
    public Account? Deleter { get; set; }
}
