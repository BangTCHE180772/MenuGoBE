using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MenuGoBE.Helpers;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Document
{
    public class ReturnDocumentCreateDto
    {
        /// <summary>
        /// Id của phiếu nhập gốc mà phiếu trả này thuộc về.
        /// </summary>
        [Required]
        public long ImportDocumentId { get; set; }

        [Required]
        public DateTime BusinessDate { get; set; }

        /// <summary>
        /// Bắt buộc: Id tài khoản người tạo phiếu (truyền từ Frontend).
        /// </summary>
        [Required]
        public long CreatedByAccountId { get; set; }

        /// <summary>
        /// Số tiền NCC đã thực sự hoàn trả. Nếu = 0 thì không sinh CashFlow.
        /// </summary>
        [Range(0, 10000000000.000, ErrorMessage = "Số tiền đã nhận không được âm")]
        [IntegerOnly(ErrorMessage = "Số tiền đã nhận phải là số nguyên.")]
        public decimal PaidAmount { get; set; } = 0m;

        /// <summary>
        /// Phương thức thanh toán (mặc định: Tiền mặt).
        /// </summary>
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        public string? Note { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm trả lại")]
        public List<ReturnDocumentDetailDto> Details { get; set; } = new List<ReturnDocumentDetailDto>();
    }

    public class ReturnDocumentDetailDto
    {
        /// <summary>
        /// Id của dòng chi tiết phiếu nhập gốc (ImportDocumentDetail.Id).
        /// Dùng để xác định chính xác hạn mức trả và kế thừa BInventoryId, UnitConversionId.
        /// </summary>
        [Required]
        public long ImportDocumentDetailId { get; set; }

        /// <summary>
        /// Số lượng muốn trả. Dòng có Quantity == 0 sẽ bị bỏ qua tự động.
        /// </summary>
        [Required]
        [Range(0, 10000000000.000, ErrorMessage = "Số lượng trả phải từ 0 đến 10.000.000.000 (10 tỷ).")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Đơn giá hoàn tiền (người dùng tự nhập cho từng dòng).
        /// </summary>
        [Required]
        [Range(1, 999999999999, ErrorMessage = "Đơn giá hoàn tiền phải là số nguyên lớn hơn hoặc bằng 1.")]
        [IntegerOnly(ErrorMessage = "Đơn giá hoàn tiền phải là số nguyên lớn hơn hoặc bằng 1.")]
        public decimal UnitPrice { get; set; }

        public string? Note { get; set; }
    }
}
