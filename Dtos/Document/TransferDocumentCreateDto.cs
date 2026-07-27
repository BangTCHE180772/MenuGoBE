using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
        /// Tùy chọn: Id tài khoản người tạo truyền trực tiếp từ Frontend (nếu có).
        /// </summary>
        public long? AccountId { get; set; }

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
        [Range(0.001, 1000000.000, ErrorMessage = "Số lượng chuyển phải từ 0,001 đến 1.000.000 đơn vị.")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Tùy chọn: Đơn giá chuyển kho cho mỗi đơn vị sản phẩm (null nếu dùng giá vốn bình quân).
        /// </summary>
        [Range(1, 999999999999, ErrorMessage = "Đơn giá phải là số nguyên lớn hơn hoặc bằng 1.")]
        public decimal? UnitPrice { get; set; }

        public string? Note { get; set; }
    }


    public class TransferDocumentPartnerDto
    {
        [Required]
        public long PartnerId { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Chi phí vận chuyển không được âm.")]
        public decimal Cost { get; set; }

        public string? Note { get; set; }
    }

    public class TransferRejectDocumentDto
    {
        [Required]
        public long TransferDocumentId { get; set; }

        /// <summary>
        /// Tùy chọn: Id tài khoản người thực hiện từ chối truyền trực tiếp từ Frontend (nếu có).
        /// </summary>
        public long? AccountId { get; set; }

        public string? Reason { get; set; }
    }

}

