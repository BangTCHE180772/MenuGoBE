using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Document
{
    public class ReturnDocumentCreateDto
    {
        [Required]
        public long ImportDocumentId { get; set; }

        [Required]
        public DateTime BusinessDate { get; set; }

        public string? Note { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm trả lại")]
        public List<ReturnDocumentDetailDto> Details { get; set; } = new List<ReturnDocumentDetailDto>();
    }

    public class ReturnDocumentDetailDto
    {
        [Required]
        public long BInventoryId { get; set; }

        [Required]
        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng trả phải lớn hơn 0")]
        public decimal Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá hoàn tiền không được âm")]
        public decimal UnitPrice { get; set; }

        public string? Note { get; set; }
    }
}
