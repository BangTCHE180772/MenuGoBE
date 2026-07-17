namespace MenuGoBE.Dtos.Voucher;

public class VoucherCheckResponseDto
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public Models.Voucher? Voucher { get; set; }
}
