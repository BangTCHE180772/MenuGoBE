using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MenuGoBE.Helpers;

namespace MenuGoBE.Dtos.Document
{
    public class ImportDocumentCreateDto
    {
        [Required]
        public long BranchId { get; set; }

        [Required]
        public DateTime BusinessDate { get; set; }

        /// <summary>
        /// Bắt buộc: Id tài khoản người tạo phiếu (truyền từ Frontend).
        /// </summary>
        [Required]
        public long CreatedByAccountId { get; set; }

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
        [Range(0.001, 10000000000.000, ErrorMessage = "Số lượng nhập phải từ 0,001 đến 10.000.000.000 (10 tỷ) đơn vị.")]
        public decimal Quantity { get; set; }

        [Required]
        [Range(1, 999999999999, ErrorMessage = "Đơn giá phải là số nguyên lớn hơn hoặc bằng 1.")]
        [IntegerOnly(ErrorMessage = "Đơn giá phải là số nguyên lớn hơn hoặc bằng 1.")]
        public decimal UnitPrice { get; set; }

        public string? Note { get; set; }
    }


    public class ImportDocumentPartnerDto : IValidatableObject
    {
        [Required]
        public long PartnerId { get; set; }

        /// <summary>
        /// Số tiền cần trả (AmountDue).
        /// - Supplier: bỏ trống (hệ thống tự tính = Tổng tiền hàng).
        /// - Transport / Other: bắt buộc nhập.
        /// </summary>
        [Range(0, 10000000000.000, ErrorMessage = "Số tiền cần trả phải lớn hơn hoặc bằng 0.")]
        [IntegerOnly(ErrorMessage = "Số tiền cần trả phải là số nguyên.")]
        public decimal? AmountDue { get; set; }

        /// <summary>
        /// Số tiền đã trả (AmountPaid). Nếu > 0 sẽ tự động sinh phiếu chi (CashFlow).
        /// </summary>
        [Range(0, 10000000000.000, ErrorMessage = "Số tiền đã trả phải lớn hơn hoặc bằng 0.")]
        [IntegerOnly(ErrorMessage = "Số tiền đã trả phải là số nguyên.")]
        public decimal AmountPaid { get; set; } = 0;

        public string? Note { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (AmountDue.HasValue && AmountPaid > AmountDue.Value)
            {
                yield return new ValidationResult(
                    $"Số tiền đã trả ({AmountPaid:N0} ₫) không được vượt quá số tiền phải trả ({AmountDue.Value:N0} ₫).",
                    new[] { nameof(AmountPaid) }
                );
            }
        }
    }
}
