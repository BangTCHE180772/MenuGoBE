using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Document
{
    /// <summary>
    /// DTO yêu cầu tạo mới Phiếu Xuất kho bán hàng (Sale Document).
    /// </summary>
    public class SaleDocumentCreateDto
    {
        [Required(ErrorMessage = "Mã chi nhánh không được để trống.")]
        public long BranchId { get; set; }

        public long? PartnerId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Danh sách chi tiết mặt hàng bán không được để trống.")]
        public List<SaleDocumentDetailCreateDto> Details { get; set; } = new List<SaleDocumentDetailCreateDto>();
    }

    /// <summary>
    /// DTO chi tiết từng dòng mặt hàng khi tạo mới Phiếu Xuất kho bán hàng.
    /// </summary>
    public class SaleDocumentDetailCreateDto
    {
        [Required(ErrorMessage = "BInventoryId không được để trống.")]
        public long BInventoryId { get; set; }

        public long? UnitConversionId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng bán phải lớn hơn 0.")]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá bán không được nhỏ hơn 0.")]
        public decimal UnitPrice { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }
}
