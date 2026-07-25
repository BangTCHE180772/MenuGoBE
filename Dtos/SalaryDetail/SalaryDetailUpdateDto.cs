using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.SalaryDetail;

public class SalaryDetailUpdateDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public SalaryAdjustmentType Type { get; set; }
    public decimal Amount { get; set; }
    public string Note { get; set; } = string.Empty;
}
