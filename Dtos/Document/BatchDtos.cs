using System;
using System.Collections.Generic;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Document
{
    #region DTO Thông tin Lô hàng (BInventoryBatch Response DTO)
    public class BatchDto
    {
        public long Id { get; set; }
        public long BInventoryId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public decimal QuantityOriginal { get; set; }
        public decimal QuantityRemaining { get; set; }
        public decimal UnitCost { get; set; }
        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime ReceivedDate { get; set; }
        public long? SourceBatchId { get; set; }
        public string? SourceBatchCode { get; set; }
        public BatchStatus Status { get; set; }
        public bool IsNotificationMuted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    #endregion

    #region DTO Chi tiết Phân bổ Lô (BatchAllocation Response DTO)
    public class BatchAllocationDto
    {
        public long Id { get; set; }
        public long DocumentDetailId { get; set; }
        public long BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public BatchAllocationType AllocationType { get; set; }
        public decimal QuantityAllocated { get; set; }
        public decimal UnitCost { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    #endregion

    #region DTO Tạo / Nhập Lô hàng từ Client (Batch Create / Import Input DTO)
    public class BatchCreateDto
    {
        public string? BatchCode { get; set; }
        public decimal Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
    #endregion

    #region DTO Kiểm kê theo từng Lô (Batch Check Item DTO)
    public class BatchCheckItemDto
    {
        public long? BatchId { get; set; }
        public string? BatchCode { get; set; }
        public decimal SystemQuantity { get; set; }
        public decimal ActualQuantity { get; set; }
        public decimal DifferenceQuantity => ActualQuantity - SystemQuantity;
        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
    #endregion

    #region DTO Chi tiết Lô hàng mở rộng (Batch Detail DTO)
    public class BatchDetailDto : BatchDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public long BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public long? SourceBranchId { get; set; }
        public string? SourceBranchName { get; set; }
        public int? DaysUntilExpiry { get; set; }
        public string ExpiryStatus { get; set; } = "Valid"; // "Expired", "NearExpiry", "Valid", "NoExpiry"
    }
    #endregion

    #region DTO Phân bổ Lô chi tiết cho Truy vết (Batch Allocation Trace DTO)
    public class BatchAllocationTraceDto
    {
        public long Id { get; set; }
        public long DocumentDetailId { get; set; }
        public long BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public BatchAllocationType AllocationType { get; set; }
        public string AllocationTypeName { get; set; } = string.Empty;
        public decimal QuantityAllocated { get; set; }
        public decimal UnitCost { get; set; }
        public DateTime CreatedAt { get; set; }

        public long? DocumentId { get; set; }
        public long? OrderId { get; set; }
        public string? DocumentCode { get; set; }
        public DocumentType? DocumentType { get; set; }
        public string? DocumentTypeName { get; set; }
        public DateTime? DocumentPostedAt { get; set; }
        public string? BranchName { get; set; }
        public string? PartnerName { get; set; }
        public string? Note { get; set; }
    }
    #endregion

    #region DTO Hồ sơ Truy vết Lô toàn diện (Batch Traceability DTO)
    public class BatchTraceabilityDto
    {
        public BatchDetailDto CurrentBatch { get; set; } = new BatchDetailDto();
        public BatchDetailDto? SourceBatch { get; set; }
        public List<BatchAllocationTraceDto> Allocations { get; set; } = new List<BatchAllocationTraceDto>();
        public long? InitialImportDocumentId { get; set; }
        public string? InitialImportDocumentCode { get; set; }
    }
    #endregion

    #region DTO Tổng hợp Thống kê Hạn sử dụng (Batch Expiry Summary DTO)
    public class BatchExpirySummaryDto
    {
        public int TotalBatches { get; set; }
        public int ActiveBatches { get; set; }
        public int ExpiredBatches { get; set; }
        public int NearExpiryBatches { get; set; }
        public int CriticalBatches { get; set; }
        public int ValidBatches { get; set; }
        public int NoExpiryBatches { get; set; }
    }
    #endregion

    #region DTO Cài đặt Thông báo Cảnh báo Hạn dùng Chi nhánh
    public class BatchAlertSettingsDto
    {
        public long BranchId { get; set; }
        public int CriticalDays { get; set; } = 7;
        public int WarningDays { get; set; } = 14;
    }
    #endregion

    #region DTO Cập nhật Ngày sản xuất và Hạn sử dụng Lô hàng
    public class UpdateBatchDatesDto
    {
        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
    #endregion
}
