using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("PayrollSuggestions")]
public class PayrollSuggestion
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    public long AccountId { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public SalaryAdjustmentType Type { get; set; } = SalaryAdjustmentType.Allowance;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ApplyOption { get; set; } = "NextMonth"; // NextMonth | CurrentMonth | ResignImmediate

    public bool IsResigned { get; set; } = false;

    public PayrollSuggestionStatus Status { get; set; } = PayrollSuggestionStatus.Pending;

    [MaxLength(1000)]
    public string? ManagerNote { get; set; }

    public long? ProcessedBy { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public bool IsApplied { get; set; } = false;

    public long? AppliedPayrollId { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("AccountId")]
    public Account Account { get; set; } = null!;

    [ForeignKey("ProcessedBy")]
    public Account? Processor { get; set; }

    [ForeignKey("CreatedBy")]
    public Account Creator { get; set; } = null!;

    [ForeignKey("AppliedPayrollId")]
    public Payroll? AppliedPayroll { get; set; }
}
