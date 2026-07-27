using MenuGoBE.Dtos.SalaryDetail;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Payroll;

public class PayrollViewDto
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public long BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public long? ContractId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string SalaryType { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public int BaseWorkDays { get; set; }
    public decimal ActualWorkDays { get; set; }
    public decimal ActualWorkHours { get; set; }
    public decimal CalculatedSalary { get; set; }
    public decimal TotalAllowance { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal TotalDeduction { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal NetSalary { get; set; }
    public PayrollStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime? PaymentDate { get; set; }
    public string Note { get; set; } = string.Empty;
    public long CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SalaryDetailViewDto> SalaryDetails { get; set; } = new();
}
