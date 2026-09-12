using System.Collections.Generic;

namespace MenuGoBE.Dtos.Document
{
    /// <summary>
    /// DTO phản hồi thông tin chi tiết phiếu Nhập kho gốc phục vụ tạo phiếu Trả hàng NCC.
    /// Bao gồm snapshot sản phẩm và thông tin tích lũy số lượng đã trả theo từng dòng nhập.
    /// </summary>
    public class ImportReturnDetailsDto
    {
        /// <summary>ID phiếu Nhập kho gốc.</summary>
        public long ImportDocumentId { get; set; }

        /// <summary>Mã phiếu Nhập kho gốc (Snapshot).</summary>
        public string ImportDocumentCode { get; set; } = string.Empty;

        /// <summary>ID Nhà cung cấp (lấy từ phiếu Nhập gốc).</summary>
        public long? PartnerId { get; set; }

        /// <summary>Tên Nhà cung cấp (Snapshot từ phiếu Nhập gốc).</summary>
        public string PartnerName { get; set; } = string.Empty;

        /// <summary>Tổng tiền của phiếu nhập gốc.</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>Số tiền cửa hàng đã thanh toán cho phiếu nhập gốc.</summary>
        public decimal AmountPaid { get; set; }

        /// <summary>Danh sách dòng sản phẩm trong phiếu Nhập gốc kèm thông tin trả hàng.</summary>
        public List<ImportReturnDetailItemDto> Details { get; set; } = new List<ImportReturnDetailItemDto>();
    }

    /// <summary>
    /// DTO từng dòng sản phẩm trong phiếu Nhập gốc dành cho tạo phiếu Trả hàng.
    /// Dữ liệu hoàn toàn từ Snapshot của phiếu Nhập đã chốt.
    /// </summary>
    public class ImportReturnDetailItemDto
    {
        /// <summary>
        /// ID dòng chi tiết phiếu Nhập gốc — dùng làm FatherId khi tạo phiếu Trả hàng.
        /// </summary>
        public long Id { get; set; }

        /// <summary>ID kho hàng BInventory tại chi nhánh (Snapshot từ phiếu Nhập).</summary>
        public long BInventoryId { get; set; }

        /// <summary>ID đơn vị quy đổi (Snapshot từ phiếu Nhập).</summary>
        public long? UnitConversionId { get; set; }

        /// <summary>Tên sản phẩm (SnapshotProductName từ phiếu Nhập).</summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>Mã sản phẩm (SnapshotProductCode từ phiếu Nhập).</summary>
        public string ProductCode { get; set; } = string.Empty;

        /// <summary>Tên đơn vị tính (SnapshotUnitName từ phiếu Nhập).</summary>
        public string UnitName { get; set; } = string.Empty;

        /// <summary>Tỷ lệ quy đổi (ConversionRate từ phiếu Nhập).</summary>
        public decimal ConversionRate { get; set; } = 1;

        /// <summary>Số lượng đã nhập theo đơn vị tính (Quantity gốc từ phiếu Nhập).</summary>
        public decimal Quantity { get; set; }

        /// <summary>Số lượng cơ sở đã nhập (BaseQuantity từ phiếu Nhập).</summary>
        public decimal BaseQuantity { get; set; }

        /// <summary>Đơn giá nhập gốc (UnitPrice Snapshot từ phiếu Nhập — dùng làm giá trả mặc định).</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Tổng số lượng (BaseQuantity) đã trả tích lũy ở các phiếu Trả hàng trước đó.
        /// </summary>
        public decimal AlreadyReturnedBaseQuantity { get; set; }

        /// <summary>
        /// Số lượng tối đa còn có thể trả (theo đơn vị tính, KHÔNG phải BaseQuantity).
        /// = (BaseQuantity_gốc - AlreadyReturnedBaseQuantity) / ConversionRate
        /// </summary>
        public decimal MaxReturnQuantity { get; set; }
    }
}
