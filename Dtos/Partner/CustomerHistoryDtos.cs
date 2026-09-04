using System;

namespace MenuGoBE.Dtos.Partner
{
    public class CustomerPurchaseHistoryDto
    {
        public long OrderId { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public int PointsEarned { get; set; }
        public int PointsUsed { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CustomerPointHistoryDto
    {
        public long Id { get; set; }
        public DateTime Time { get; set; }
        public string Type { get; set; } = string.Empty; // "Tích điểm" | "Sử dụng điểm" | "Điều chỉnh"
        public string RelatedCode { get; set; } = string.Empty;
        public int PointChange { get; set; }
        public int PointBefore { get; set; }
        public int PointAfter { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
