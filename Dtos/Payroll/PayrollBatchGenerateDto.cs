namespace MenuGoBE.Dtos.Payroll;

public class PayrollBatchGenerateDto
{
    public long? BranchId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public long CreatedBy { get; set; }
}
