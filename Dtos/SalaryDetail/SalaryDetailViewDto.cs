using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.SalaryDetail;

public class SalaryDetailViewDto
{
    public long Id { get; set; }
    public long PayrollId { get; set; }
    public long AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public SalaryAdjustmentType Type { get; set; }
    public string TypeName => Type.ToString();
    public decimal Amount { get; set; }
    public string Note { get; set; } = string.Empty;
    public long CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
