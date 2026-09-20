using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Document
{
    /// <summary>
    /// DTO yêu cầu tạo mới Phiếu Chuyển kho (Transfer Document).
    /// </summary>
    public class TransferDocumentCreateDto
    {
        public long BranchId { get; set; }

        [Required(ErrorMessage = "Mã chi nhánh nhận không được để trống.")]
        public long ToBranchId { get; set; }

        public string? Code { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Danh sách chi tiết mặt hàng chuyển kho không được để trống.")]
        public List<TransferDocumentDetailCreateDto> Details { get; set; } = new List<TransferDocumentDetailCreateDto>();
    }

    /// <summary>
    /// DTO dòng mặt hàng khi tạo mới Phiếu Chuyển kho.
    /// </summary>
    public class TransferDocumentDetailCreateDto
    {
        [Required(ErrorMessage = "BInventoryId không được để trống.")]
        public long BInventoryId { get; set; }

        public long? UnitConversionId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng chuyển phải lớn hơn 0.")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Đơn giá chuyển kho (theo đơn vị tính đã chọn). Nếu = 0 thì hệ thống tự dùng giá vốn trung bình snapshot.
        /// </summary>
        [Range(0, 10_000_000_000, ErrorMessage = "Đơn giá chuyển phải từ 0 đến 10 tỷ VNĐ.")]
        public decimal UnitPrice { get; set; } = 0;

        [MaxLength(255)]
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO yêu cầu cập nhật Phiếu Chuyển kho PENDING.
    /// </summary>
    public class TransferDocumentUpdateDto
    {
        [Required(ErrorMessage = "Mã chi nhánh nhận không được để trống.")]
        public long ToBranchId { get; set; }

        public string? Code { get; set; }

        public DateTime OrderDate { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Danh sách chi tiết mặt hàng chuyển kho không được để trống.")]
        public List<TransferDocumentDetailUpdateDto> Details { get; set; } = new List<TransferDocumentDetailUpdateDto>();
    }

    /// <summary>
    /// DTO dòng mặt hàng khi cập nhật Phiếu Chuyển kho PENDING.
    /// </summary>
    public class TransferDocumentDetailUpdateDto
    {
        public long? Id { get; set; }

        [Required(ErrorMessage = "BInventoryId không được để trống.")]
        public long BInventoryId { get; set; }

        public long? UnitConversionId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng chuyển phải lớn hơn 0.")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Đơn giá chuyển kho (theo đơn vị tính đã chọn). Nếu = 0 thì hệ thống tự dùng giá vốn trung bình snapshot.
        /// </summary>
        [Range(0, 10_000_000_000, ErrorMessage = "Đơn giá chuyển phải từ 0 đến 10 tỷ VNĐ.")]
        public decimal UnitPrice { get; set; } = 0;

        [MaxLength(255)]
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO yêu cầu xử lý nhận hàng / từ chối nhận hàng tại chi nhánh nhận.
    /// </summary>
    public class TransferReceiptRequestDto
    {
        [Required(ErrorMessage = "Trạng thái nhận hàng không được để trống.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DocumentTransferStatus Status { get; set; }

        public List<TransferReceiptDetailDto>? ReceivedDetails { get; set; }

        [Required(ErrorMessage = "Bắt buộc phải nhập Ghi chú (Note) khi xác nhận nhận hàng.")]
        [MaxLength(255)]
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO số lượng thực nhận từng dòng khi chi nhánh nhận xác nhận.
    /// </summary>
    public class TransferReceiptDetailDto
    {
        public long DetailId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số lượng thực nhận không được nhỏ hơn 0.")]
        public decimal ReceivedQuantity { get; set; }
    }
}

