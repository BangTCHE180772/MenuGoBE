using MenuGoBE.Dtos.SalaryDetail;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Payroll;

public class PayrollUpdateDto
{
    public long Id { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string SalaryType { get; set; } = "Monthly";
    public decimal BaseSalary { get; set; }
    public int BaseWorkDays { get; set; } = 26;
    public decimal ActualWorkDays { get; set; }
    public decimal ActualWorkHours { get; set; }
    public decimal CalculatedSalary { get; set; }
    public decimal TotalAllowance { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal TotalDeduction { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal NetSalary { get; set; }
    public PayrollStatus Status { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<SalaryDetailCreateDto> SalaryDetails { get; set; } = new();
}
