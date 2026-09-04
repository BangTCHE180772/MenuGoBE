namespace MenuGoBE.Models.Enums;

public enum PointTransactionType
{
    /// <summary>
    /// Tích điểm từ thanh toán hóa đơn
    /// </summary>
    Earn = 1,

    /// <summary>
    /// Sử dụng điểm khi thanh toán
    /// </summary>
    Redeem = 2,

    /// <summary>
    /// Nhân viên điều chỉnh điểm thủ công
    /// </summary>
    ManualAdjustment = 3,

    /// <summary>
    /// Hoàn tác giao dịch điều chỉnh thủ công
    /// </summary>
    Reversal = 4
}
