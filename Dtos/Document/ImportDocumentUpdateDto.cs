using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Document
{
    /// <summary>
    /// DTO yêu cầu cập nhật Phiếu nhập kho đang ở trạng thái PENDING.
    /// </summary>
    public class ImportDocumentUpdateDto
    {
        public string? Code { get; set; }

        public long? PartnerId { get; set; }

        public DateTime OrderDate { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }

        #region Thông tin Thanh toán Cập nhật
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
        public List<ImportDocumentDetailUpdateDto> Details { get; set; } = new List<ImportDocumentDetailUpdateDto>();
    }

    /// <summary>
    /// DTO thông tin từng dòng sản phẩm khi cập nhật Phiếu nhập kho PENDING.
    /// </summary>
    public class ImportDocumentDetailUpdateDto
    {
        /// <summary>
        /// ID của dòng chi tiết đã có (Null nếu là dòng mới thêm vào phiếu)
        /// </summary>
        public long? Id { get; set; }

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
