using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Document
{
    public class CheckDocumentCreateDto
    {
        [Required]
        public long BranchId { get; set; }

        [Required]
        public DateTime BusinessDate { get; set; }

        /// <summary>
        /// Tùy chọn: Id tài khoản người tạo truyền trực tiếp từ Frontend (nếu có).
        /// </summary>
        public long? AccountId { get; set; }

        public string? Note { get; set; }


        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 chi tiết kiểm kho.")]
        public List<CheckDocumentDetailDto> Details { get; set; } = new List<CheckDocumentDetailDto>();
    }

    public class CheckDocumentDetailDto
    {
        [Required]
        public long BInventoryId { get; set; }

        [Required]
        [Range(0, 10000000000.000, ErrorMessage = "Số lượng kiểm đếm thực tế không hợp lệ (từ 0 đến 10.000.000.000).")]
        public decimal ActualQuantity { get; set; }

        [Range(1, 999999999999, ErrorMessage = "Đơn giá phải là số nguyên lớn hơn hoặc bằng 1.")]
        public decimal? UnitPrice { get; set; }

        public string? Note { get; set; }
    }

}
