using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MenuGoBE.Helpers;

namespace MenuGoBE.Dtos.Document
{
    public class TransferDocumentCreateDto
    {
        [Required]
        public long FromBranchId { get; set; }

        [Required]
        public long ToBranchId { get; set; }

        [Required]
        public DateTime BusinessDate { get; set; }

        /// <summary>
        /// Bắt buộc: Id tài khoản người tạo phiếu (truyền từ Frontend).
        /// </summary>
        [Required]
        public long CreatedByAccountId { get; set; }

        public string? Note { get; set; }


        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm chuyển kho.")]
        public List<TransferDocumentDetailDto> Details { get; set; } = new List<TransferDocumentDetailDto>();

        public List<TransferDocumentPartnerDto> Partners { get; set; } = new List<TransferDocumentPartnerDto>();
    }

    public class TransferDocumentDetailDto
    {
        [Required]
        public long FromBInventoryId { get; set; }

        [Required]
        public long ToBInventoryId { get; set; }

        /// <summary>
        /// Tùy chọn: Id của UnitConversion áp dụng cho dòng chuyển kho này (null nếu dùng đơn vị base).
        /// </summary>
        public long? UnitConversionId { get; set; }

        [Required]
        [Range(0.001, 10000000000.000, ErrorMessage = "Số lượng chuyển phải từ 0,001 đến 10.000.000.000 (10 tỷ).")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Tùy chọn: Đơn giá chuyển kho cho mỗi đơn vị sản phẩm (null nếu dùng giá vốn bình quân).
        /// </summary>
        [Range(1, 999999999999, ErrorMessage = "Đơn giá phải là số nguyên lớn hơn hoặc bằng 1.")]
        [IntegerOnly(ErrorMessage = "Đơn giá phải là số nguyên lớn hơn hoặc bằng 1.")]
        public decimal? UnitPrice { get; set; }

        public string? Note { get; set; }
    }


    public class TransferDocumentPartnerDto
    {
        [Required]
        public long PartnerId { get; set; }

        [Required]
        [Range(0, 10000000000.000, ErrorMessage = "Chi phí vận chuyển không được âm.")]
        [IntegerOnly(ErrorMessage = "Chi phí vận chuyển phải là số nguyên.")]
        public decimal Cost { get; set; }

        public string? Note { get; set; }
    }

    public class TransferRejectDocumentDto
    {
        [Required]
        public long TransferDocumentId { get; set; }

        /// <summary>
        /// Bắt buộc: Id tài khoản người thực hiện từ chối (truyền từ Frontend).
        /// </summary>
        [Required]
        public long CreatedByAccountId { get; set; }

        public string? Reason { get; set; }
    }

}
