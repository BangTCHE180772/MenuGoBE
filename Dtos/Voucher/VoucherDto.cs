namespace MenuGoBE.Dtos.Voucher;

public class VoucherDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal MaxDiscount { get; set; }
    public decimal MinOrderValue { get; set; }
    public long? BranchId { get; set; }
    public string? BranchName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Quantity { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
    public int? MaxUsagePerCustomer { get; set; }
}
