namespace MenuGoBE.Dtos.Payroll;

public class PayrollGenerateDto
{
    public long AccountId { get; set; }
    public long BranchId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public long CreatedBy { get; set; }
}
