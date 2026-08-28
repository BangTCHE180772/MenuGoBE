namespace MenuGoBE.Dtos.Payroll;

public class PayrollShiftDetailViewDto
{
    public long Id { get; set; }
    public long PayrollId { get; set; }
    public long WorkScheduleId { get; set; }
    public DateOnly WorkDate { get; set; }
    public decimal ActualHours { get; set; }
    public decimal StandardHours { get; set; }
    public decimal ApprovedOTHours { get; set; }
    public decimal DateCoefficient { get; set; }
    public bool IsNightShift { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal BaseShiftPay { get; set; }
    public decimal NightShiftPay { get; set; }
    public decimal OTPay { get; set; }
    public decimal TotalShiftPay { get; set; }
    public string Note { get; set; } = string.Empty;
}
