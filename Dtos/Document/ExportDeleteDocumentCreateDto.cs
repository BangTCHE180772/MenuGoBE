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
        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng xuất hủy phải lớn hơn 0.")]
        public decimal Quantity { get; set; }

        public string? Note { get; set; }
    }
}
