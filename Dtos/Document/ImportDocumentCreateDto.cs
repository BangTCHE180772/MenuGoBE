using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Document
{
    /// <summary>
    /// DTO yêu cầu tạo mới Phiếu nhập kho (Import Document).
    /// Hỗ trợ cả hai hình thức: Lưu tạm (Pending) hoặc Chốt trực tiếp (Completed).
    /// </summary>
    public class ImportDocumentCreateDto
    {
        public string? Code { get; set; }

        [Required(ErrorMessage = "Mã chi nhánh không được để trống.")]
        public long BranchId { get; set; }

        public long? PartnerId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? Note { get; set; }

        #region Thông tin Thanh toán Ban đầu (Tùy chọn)
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        public decimal AmountPaid { get; set; } = 0;

        public decimal DebtDeductionAmount { get; set; } = 0;
        #endregion

        /// <summary>
        /// Danh sách URL hình ảnh chứng từ/biên lai/bằng chứng nhận hàng
        /// </summary>
        public List<string>? ImageUrls { get; set; }

        [Required(ErrorMessage = "Danh sách chi tiết mặt hàng nhập kho không được để trống.")]
        public List<ImportDocumentDetailCreateDto> Details { get; set; } = new List<ImportDocumentDetailCreateDto>();
    }

    /// <summary>
    /// DTO thông tin từng dòng sản phẩm khi tạo Phiếu nhập kho.
    /// </summary>
    public class ImportDocumentDetailCreateDto
    {
        [Required(ErrorMessage = "BInventoryId không được để trống.")]
        public long BInventoryId { get; set; }

        public long? UnitConversionId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng nhập phải lớn hơn 0.")]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá nhập không được nhỏ hơn 0.")]
        public decimal UnitPrice { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }

        [MaxLength(100)]
        public string? BatchCodeSnapshot { get; set; }

        public DateTime? ManufactureDateSnapshot { get; set; }

        public DateTime? ExpiryDateSnapshot { get; set; }
    }
}
