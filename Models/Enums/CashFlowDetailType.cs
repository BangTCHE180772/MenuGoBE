namespace MenuGoBE.Models.Enums;

public enum CashFlowDetailType
{
    Payment = 1,
    Refund = 2,
    Advance = 3,
    Deduction = 4,

    /// <summary>
    /// Dòng điều chỉnh làm tròn: ghi nhận phần chênh lệch giữa OriginalAmount (chưa làm tròn)
    /// và RoundedAmount (số tiền thực tế thanh toán/nhận từ NCC).
    /// RoundingValue = OriginalAmount - RoundedAmount (có thể dương hoặc âm).
    /// </summary>
    Rounding = 5
}
