namespace MenuGoBE.Dtos.Kitchen;

public class RequestRestockDto
{
    public long BranchId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; }
}
