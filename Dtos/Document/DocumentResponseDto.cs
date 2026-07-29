using System;
using System.Collections.Generic;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Document
{
    public class DocumentResponseDto
    {
        public long Id { get; set; }
        public long BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public long? ParentDocumentId { get; set; }
        public long PostingSequence { get; set; }
        public DateTime BusinessDate { get; set; }
        public string Code { get; set; } = string.Empty;
        public DocumentType Type { get; set; }
        public string TypeLabel { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public string TransferStatus { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Creator { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public List<DocumentDetailResponseDto> Details { get; set; } = new List<DocumentDetailResponseDto>();
        public List<DocumentPartnerResponseDto> Partners { get; set; } = new List<DocumentPartnerResponseDto>();
        public List<CashFlowDetailResponseDto> CashFlowDetails { get; set; } = new List<CashFlowDetailResponseDto>();
        public List<DocumentResponseDto> PreviousReturns { get; set; } = new List<DocumentResponseDto>();
    }

    public class DocumentDetailResponseDto
    {
        public long Id { get; set; }
        public long BInventoryId { get; set; }
        public long ProductId { get; set; }
        public long? UnitConversionId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal SystemQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal AlreadyReturnedQuantity { get; set; }
        public decimal MaxReturnQuantity { get; set; }
        public string? Note { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class DocumentPartnerResponseDto
    {
        public long Id { get; set; }
        public long PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public PartnerType PartnerType { get; set; }
        public decimal Cost { get; set; }
        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; }
        public string? Note { get; set; }

        public List<CashFlowDetailResponseDto> CashFlowDetails { get; set; } = new List<CashFlowDetailResponseDto>();
    }

    public class CashFlowDetailResponseDto
    {
        public long Id { get; set; }
        public long CashFlowId { get; set; }
        public string CashFlowCode { get; set; } = string.Empty;
        public CashFlowDetailType Type { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime BusinessDate { get; set; }
        public string? Note { get; set; }
    }
}
