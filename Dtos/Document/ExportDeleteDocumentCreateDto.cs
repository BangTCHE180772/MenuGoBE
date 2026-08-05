using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Document
{
    public class ExportDeleteDocumentCreateDto
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
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm xuất hủy.")]
        public List<ExportDeleteDocumentDetailDto> Details { get; set; } = new List<ExportDeleteDocumentDetailDto>();
    }

    public class ExportDeleteDocumentDetailDto
    {
        [Required]
        public long BInventoryId { get; set; }

        [Required]
        [Range(0.001, 10000000000.000, ErrorMessage = "Số lượng xuất hủy phải từ 0,001 đến 10.000.000.000 (10 tỷ) đơn vị.")]
        public decimal Quantity { get; set; }

        public string? Note { get; set; }
    }


}
