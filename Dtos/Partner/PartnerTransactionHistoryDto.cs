using System;

namespace MenuGoBE.Dtos.Partner
{
    public class PartnerTransactionHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } = string.Empty; // "Nhập hàng" | "Thanh toán NCC" | "Trả hàng nhập"
        public string Code { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Note { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class PartnerImportDocumentDto
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DateTime ImportDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal DebtAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
