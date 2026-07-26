using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Document
{
    public class AdjustmentDocumentCreateDto
    {
        [Required]
        public long BranchId { get; set; }

        [Required]
        public DateTime BusinessDate { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 chi tiết kiểm kho.")]
        public List<AdjustmentDocumentDetailDto> Details { get; set; } = new List<AdjustmentDocumentDetailDto>();
    }

    public class AdjustmentDocumentDetailDto
    {
        [Required]
        public long BInventoryId { get; set; }

        [Required]
        [Range(0, 99999999.999, ErrorMessage = "Số lượng kiểm đếm thực tế không hợp lệ (>= 0).")]
        public decimal ActualQuantity { get; set; }

        [Range(0, 99999999999.999, ErrorMessage = "Đơn giá không hợp lệ (>= 0).")]
        public decimal? UnitPrice { get; set; }

        public string? Note { get; set; }
    }
}
