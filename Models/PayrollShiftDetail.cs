using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("PayrollShiftDetails")]
public class PayrollShiftDetail
{
    [Key]
    public long Id { get; set; }

    public long PayrollId { get; set; }

    public long WorkScheduleId { get; set; }

    public DateOnly WorkDate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ActualHours { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal StandardHours { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ApprovedOTHours { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DateCoefficient { get; set; } // e.g. 1.0 (Normal), 1.5 (Weekend), 3.0 (Tet)

    public bool IsNightShift { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal HourlyRate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal BaseShiftPay { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal NightShiftPay { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal OTPay { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalShiftPay { get; set; }

    public string Note { get; set; } = string.Empty;

    // Navigation
    [ForeignKey("PayrollId")]
    public Payroll Payroll { get; set; } = null!;

    [ForeignKey("WorkScheduleId")]
    public WorkSchedule WorkSchedule { get; set; } = null!;
}
