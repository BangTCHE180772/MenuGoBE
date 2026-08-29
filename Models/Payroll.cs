using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("Payrolls")]
public class Payroll
{
    [Key]
    public long Id { get; set; }

    public long AccountId { get; set; }

    public long BranchId { get; set; }

    public long? ContractId { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public string SalaryType { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,4)")]
    public decimal HourlyRate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalBaseShiftPay { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalNightPay { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalOTPay { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalHolidayBonus { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal BaseSalary { get; set; }

    public int BaseWorkDays { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ActualWorkDays { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ActualWorkHours { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal CalculatedSalary { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalAllowance { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal BonusAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalDeduction { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal PenaltyAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal NetSalary { get; set; }

    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;

    public DateTime? PaymentDate { get; set; }

    public string Note { get; set; } = string.Empty;

    // Audit fields for locking/approval
    public long? LockedBy { get; set; }
    public DateTime? LockedAt { get; set; }

    public long? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("AccountId")]
    public Account Account { get; set; } = null!;

    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("ContractId")]
    public Contract? Contract { get; set; }

    [ForeignKey("LockedBy")]
    public Account? Locker { get; set; }

    [ForeignKey("ApprovedBy")]
    public Account? Approver { get; set; }

    public ICollection<SalaryDetail> SalaryDetails { get; set; } = new List<SalaryDetail>();

    public ICollection<PayrollShiftDetail> PayrollShiftDetails { get; set; } = new List<PayrollShiftDetail>();
}
