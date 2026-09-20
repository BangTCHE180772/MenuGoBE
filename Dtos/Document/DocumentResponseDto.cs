using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Document
{
    #region 1. Document List Response DTO (Màn hình Danh sách - Table Grid View)
    /// <summary>
    /// DTO dùng chung cho màn hình danh sách của tất cả các loại chứng từ.
    /// Giúp truy vấn danh sách cực nhẹ và nhanh (Không mang theo danh sách dòng chi tiết).
    /// </summary>
    public class DocumentListResponseDto
    {
        public long Id { get; set; }
        public long BranchId { get; set; }
        public long? ToBranchId { get; set; }
        public long? PartnerId { get; set; }
        public long? ParentDocumentId { get; set; }
        public string? ParentDocumentCode { get; set; }
        public long PostingSequence { get; set; }

        public string Code { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DocumentType Type { get; set; }
        public string TypeName => Type.ToString();

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DocumentStatus Status { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DocumentTransferStatus TransferStatus { get; set; }

        public DateTime OrderDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResponseDate { get; set; }

        public long? CreatedBy { get; set; }
        public string? SnapshotCreatedByName { get; set; }
        public string? CurrentCreatedByName { get; set; }
        public string CreatedByName => !string.IsNullOrWhiteSpace(SnapshotCreatedByName) ? SnapshotCreatedByName : CurrentCreatedByName ?? string.Empty;

        public string? SnapshotBranchName { get; set; }
        public string? CurrentBranchName { get; set; }
        public string BranchName => !string.IsNullOrWhiteSpace(SnapshotBranchName) ? SnapshotBranchName : CurrentBranchName ?? string.Empty;

        public string? SnapshotToBranchName { get; set; }
        public string? CurrentToBranchName { get; set; }
        public string ToBranchName => !string.IsNullOrWhiteSpace(SnapshotToBranchName) ? SnapshotToBranchName : CurrentToBranchName ?? string.Empty;

        public string? SnapshotPartnerName { get; set; }
        public string? CurrentPartnerName { get; set; }
        public string PartnerName => !string.IsNullOrWhiteSpace(SnapshotPartnerName) ? SnapshotPartnerName : CurrentPartnerName ?? string.Empty;

        public decimal TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal? AmountDue { get; set; }
        public string? Note { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
    }
    #endregion

    #region 2. Document Detail Response DTO (Màn hình Xem Chi tiết Dùng chung)
    /// <summary>
    /// DTO dùng chung cho kết quả phản hồi Chi tiết của tất cả các loại chứng từ (Import, Export, Return, Transfer,...).
    /// Tích hợp đầy đủ Header, Partner, Lịch sử CashFlows và Danh sách dòng Details (Snapshot nếu Completed/Canceled, BInventory -> Product -> UnitConversions -> Unit nếu Pending).
    /// </summary>
    public class DocumentResponseDto
    {
        #region Thông tin Định danh & Liên kết
        public long Id { get; set; }
        public long BranchId { get; set; }
        public long? ToBranchId { get; set; }
        public long? PartnerId { get; set; }
        public long? ParentDocumentId { get; set; }
        public string? ParentDocumentCode { get; set; }
        public long PostingSequence { get; set; }
        #endregion

        #region Thông tin Mã & Trạng thái
        public string Code { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DocumentType Type { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DocumentStatus Status { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DocumentTransferStatus TransferStatus { get; set; }
        public DateTime OrderDate { get; set; }
        #endregion

        #region Thông tin Chi nhánh & Đối tác (Snapshot & Fallback Current)
        public string? SnapshotBranchName { get; set; }
        public string? SnapshotToBranchName { get; set; }
        public string? SnapshotPartnerName { get; set; }

        /// <summary>
        /// Tên đối tác/NCC (Tự động lấy Snapshot nếu đã chốt, hoặc lấy thông tin Partner hiện tại nếu ở dạng Pending)
        /// </summary>
        public string PartnerName => !string.IsNullOrWhiteSpace(SnapshotPartnerName) ? SnapshotPartnerName : (Partner?.Name ?? CurrentPartnerName ?? string.Empty);
        public string? CurrentPartnerName { get; set; }

        /// <summary>
        /// Tên chi nhánh (Tự động lấy Snapshot nếu đã chốt, hoặc lấy thông tin Branch hiện tại nếu ở dạng Pending)
        /// </summary>
        public string BranchName => !string.IsNullOrWhiteSpace(SnapshotBranchName) ? SnapshotBranchName : CurrentBranchName ?? string.Empty;
        public string? CurrentBranchName { get; set; }

        /// <summary>
        /// Thông tin chi tiết Nhà cung cấp / Đối tác liên kết (Được nạp đầy đủ CashFlows & Partner)
        /// </summary>
        public DocumentPartnerResponseDto? Partner { get; set; }
        #endregion

        #region Thông tin Audit Người dùng (Tạo, Chốt, Xóa, Nhận)
        public long? CreatedBy { get; set; }
        public string? SnapshotCreatedByName { get; set; }
        public string? SnapshotCreatedByUsername { get; set; }
        public DateTime CreatedAt { get; set; }

        public long? PostedBy { get; set; }
        public string? SnapshotPostedByName { get; set; }
        public string? SnapshotPostedByUsername { get; set; }
        public DateTime? PostedAt { get; set; }

        public long? DeletedBy { get; set; }
        public string? SnapshotDeletedByName { get; set; }
        public DateTime? DeletedAt { get; set; }

        public long? ReceiverAccountId { get; set; }
        public string? SnapshotReceiverName { get; set; }
        public string? SnapshotReceiverUsername { get; set; }
        public DateTime? ResponseDate { get; set; }
        #endregion

        #region Thông tin Tài chính & Ghi chú
        public decimal TotalAmount { get; set; }
        public decimal? AmountDue { get; set; }
        public decimal AmountPaid { get; set; }
        public string? Note { get; set; }
        public string? DeleteNote { get; set; }
        #endregion

        #region Lịch sử Thu/Chi & Thanh toán (CashFlows)
        /// <summary>
        /// Danh sách đầy đủ các phiếu thu/chi (CashFlows) liên quan đến chứng từ này
        /// </summary>
        public List<DocumentCashFlowResponseDto> CashFlows { get; set; } = new List<DocumentCashFlowResponseDto>();
        #endregion

        #region Danh sách Dòng chi tiết Chứng từ
        public List<DocumentDetailResponseDto> Details { get; set; } = new List<DocumentDetailResponseDto>();
        #endregion

        #region Lịch sử Thẻ Sổ kho (InventoryLedgers)
        /// <summary>
        /// Danh sách các bản ghi Sổ kho (InventoryLedgers) phát sinh từ chứng từ này
        /// </summary>
        public List<DocumentInventoryLedgerResponseDto> InventoryLedgers { get; set; } = new List<DocumentInventoryLedgerResponseDto>();
        #endregion

        #region Danh sách Hình ảnh Chứng từ
        /// <summary>
        /// Danh sách hình ảnh biên lai, hóa đơn, bằng chứng chất lượng hàng hóa khi nhận
        /// </summary>
        public List<string> ImageUrls { get; set; } = new List<string>();
        #endregion
    }
    #endregion

    #region 3. Document Item Detail Response DTO (Thông tin dòng sản phẩm)
    /// <summary>
    /// DTO phản hồi thông tin chi tiết từng dòng mặt hàng trong chứng từ.
    /// Hỗ trợ trả về cả Snapshot (khi Completed/Canceled) và Thông tin sản phẩm hiện tại + BInventory + UnitConversions + Unit (khi Pending).
    /// </summary>
    public class DocumentDetailResponseDto
    {
        #region Định danh dòng & Liên kết
        public long Id { get; set; }
        public long DocumentId { get; set; }
        public long BInventoryId { get; set; }
        public long? FatherId { get; set; }
        public long? UnitConversionId { get; set; }
        public long? ProductId { get; set; }
        #endregion

        #region Thông tin Snapshot (Khi chứng từ đã Completed / Cancelled)
        public string SnapshotProductName { get; set; } = string.Empty;
        public string SnapshotProductCode { get; set; } = string.Empty;
        public string SnapshotUnitName { get; set; } = string.Empty;
        public string SnapshotBaseUnitName { get; set; } = string.Empty;
        public decimal SnapshotAvgCost { get; set; }
        public string? BatchCodeSnapshot { get; set; }
        public DateTime? ManufactureDateSnapshot { get; set; }
        public DateTime? ExpiryDateSnapshot { get; set; }
        #endregion

        #region Thông tin Sản phẩm Hiện tại (Dành cho phiếu Pending khi Snapshot bị trống)
        public string? CurrentProductName { get; set; }
        public string? CurrentProductCode { get; set; }
        public string? CurrentUnitName { get; set; }
        public string? CurrentBaseUnitName { get; set; }
        public decimal? CurrentConversionRate { get; set; }
        public decimal? CurrentStockQuantity { get; set; }
        #endregion

        #region Nested DTOs (Dành cho phiếu Pending cần BInventory -> Product -> UnitConversions -> Unit)
        public DocumentBInventoryResponseDto? BInventory { get; set; }
        #endregion

        #region Thuộc tính Hiển thị Tự động Fallback (Cho Client)
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string BaseUnitName { get; set; } = string.Empty;
        #endregion

        #region Số lượng & Giá trị Quy đổi
        public decimal ConversionRate { get; set; } = 1;
        public decimal Quantity { get; set; }
        public decimal BaseQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        #endregion

        #region Số liệu Đặc thù Nghiệp vụ (Kiểm kho, Điều chỉnh GV, Chuyển kho)
        public decimal? SystemQuantity { get; set; }
        public decimal? ActualQuantity { get; set; }
        public decimal? AdjustedCostDelta { get; set; }
        public decimal? NewAvgCost { get; set; }
        public decimal? ReceivedQuantity { get; set; }
        #endregion

        #region Trạng thái & Ghi chú
        public bool IsDeleted { get; set; } = false;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        #endregion
    }
    #endregion

    #region 4. Document Partner Sub-DTO (Thông tin Đối tác / Nhà cung cấp)
    /// <summary>
    /// DTO thông tin Đối tác/NCC hiển thị trong chứng từ
    /// </summary>
    public class DocumentPartnerResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PartnerType Type { get; set; }
    }
    #endregion

    #region 5. Document CashFlow Sub-DTO (Lịch sử Thu/Chi - Thanh toán)
    /// <summary>
    /// DTO lịch sử giao dịch thu/chi liên quan đến chứng từ
    /// </summary>
    public class DocumentCashFlowResponseDto
    {
        public long Id { get; set; }
        public long BranchId { get; set; }
        public long? DocumentId { get; set; }
        public long? PartnerId { get; set; }
        public long PostingSequence { get; set; }
        public string Code { get; set; } = string.Empty;
        public DateTime BusinessDate { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CashFlowDirection Direction { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CashFlowDetailType Type { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentMethod PaymentMethod { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CashFlowStatus Status { get; set; }

        public decimal TotalAmount { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    #endregion

    #region 5.5 Document InventoryLedger Sub-DTO (Lịch sử Biến động Kho - Sổ kho)
    /// <summary>
    /// DTO lịch sử các bản ghi sổ kho (InventoryLedgers) gắn liền với chứng từ
    /// </summary>
    public class DocumentInventoryLedgerResponseDto
    {
        public long Id { get; set; }
        public long BInventoryId { get; set; }
        public long? DocumentId { get; set; }
        public long PostingSequence { get; set; }
        public DateTime PostedAt { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DocumentType DocumentType { get; set; }

        public long PostedBy { get; set; }
        public string SnapshotPostedByName { get; set; } = string.Empty;
        public string SnapshotBranchName { get; set; } = string.Empty;
        public string SnapshotProductName { get; set; } = string.Empty;
        public string SnapshotUnitName { get; set; } = string.Empty;
        public decimal ConversionRateSnapshot { get; set; }

        public decimal QuantityDelta { get; set; }
        public decimal InventoryValueDelta { get; set; }
        public decimal RunningQuantity { get; set; }
        public decimal RunningInventoryValue { get; set; }
        public decimal RunningAverageCost { get; set; }
        public decimal UnitCost { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    #endregion

    #region 6. Document BInventory & Product Nested DTOs (Phục vụ phiếu Pending cần live master data)
    public class DocumentBInventoryResponseDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public long BranchId { get; set; }
        public decimal Quantity { get; set; }
        public decimal Avg { get; set; }
        public DocumentProductResponseDto? Product { get; set; }
    }

    public class DocumentProductResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SKUCode { get; set; }
        public decimal MinStorage { get; set; }
        public decimal MaxStorage { get; set; }
        public decimal SellPrice { get; set; }
        public List<DocumentUnitConversionResponseDto> UnitConversions { get; set; } = new List<DocumentUnitConversionResponseDto>();
    }

    public class DocumentUnitConversionResponseDto
    {
        public long Id { get; set; }
        public long? BaseId { get; set; }
        public long UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public decimal ConversionPoint { get; set; }
    }
    #endregion
}
