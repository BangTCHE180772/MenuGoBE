using System;

namespace MenuGoBE.Dtos.Partner
{
    public class PartnerTransactionHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } = string.Empty; // "Nhập hàng" | "Thanh toán NCC" | "Trả hàng nhập"
        public string Code { get; set; } = string.Empty;
        public string? ImportCode { get; set; } // Mã phiếu nhập hàng
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
        public decimal ReturnedAmount { get; set; }
        public decimal EffectivePayableAmount { get; set; }
        public decimal DebtAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public List<PartnerTransactionHistoryDto> Transactions { get; set; } = new List<PartnerTransactionHistoryDto>();
    }

    public class PartnerPayDocumentDto
    {
        public decimal Amount { get; set; }
        public int PaymentMethod { get; set; } = 1; // 1 = Cash, 2 = BankTransfer, 3 = Other
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }
    }
}
