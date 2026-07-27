using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Document
{
    public class ImportDocumentCreateDto
    {
        [Required]
        public long BranchId { get; set; }

        [Required]
        public DateTime BusinessDate { get; set; }

        public string? Note { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm nhập kho")]
        public List<ImportDocumentDetailDto> Details { get; set; } = new List<ImportDocumentDetailDto>();

        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 đối tác (Supplier)")]
        public List<ImportDocumentPartnerDto> Partners { get; set; } = new List<ImportDocumentPartnerDto>();
    }

    public class ImportDocumentDetailDto
    {
        [Required]
        public long BInventoryId { get; set; }

        /// <summary>
        /// Id của UnitConversion (tùy chọn). Nếu có, QuantityDelta = Quantity * ConversionPoint.
        /// </summary>
        public long? UnitConversionId { get; set; }

        [Required]
        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng nhập phải lớn hơn 0")]
        public decimal Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không được âm")]
        public decimal UnitPrice { get; set; }

        public string? Note { get; set; }
    }

    public class ImportDocumentPartnerDto
    {
        [Required]
        public long PartnerId { get; set; }

        /// <summary>
        /// Số tiền cần trả (AmountDue).
        /// - Supplier: bỏ trống (hệ thống tự tính = Tổng tiền hàng).
        /// - Transport / Other: bắt buộc nhập.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Số tiền cần trả không được âm")]
        public decimal? AmountDue { get; set; }

        /// <summary>
        /// Số tiền đã trả (AmountPaid). Nếu > 0 sẽ tự động sinh phiếu chi (CashFlow).
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Số tiền đã trả không được âm")]
        public decimal AmountPaid { get; set; } = 0;

        public string? Note { get; set; }
    }
}
