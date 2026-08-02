using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Document
{
    /// <summary>
    /// DTO yêu cầu tạo mới Phiếu Xuất dùng nội bộ (Export Document).
    /// </summary>
    public class ExportDocumentCreateDto
    {
        [Required(ErrorMessage = "Mã chi nhánh không được để trống.")]
        public long BranchId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Danh sách chi tiết mặt hàng xuất không được để trống.")]
        public List<ExportDocumentDetailCreateDto> Details { get; set; } = new List<ExportDocumentDetailCreateDto>();
    }

    /// <summary>
    /// DTO dòng mặt hàng khi tạo mới Phiếu Xuất dùng nội bộ.
    /// </summary>
    public class ExportDocumentDetailCreateDto
    {
        [Required(ErrorMessage = "BInventoryId không được để trống.")]
        public long BInventoryId { get; set; }

        public long? UnitConversionId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng xuất phải lớn hơn 0.")]
        public decimal Quantity { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO yêu cầu cập nhật Phiếu Xuất dùng nội bộ PENDING.
    /// </summary>
    public class ExportDocumentUpdateDto
    {
        public DateTime OrderDate { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Danh sách chi tiết mặt hàng xuất không được để trống.")]
        public List<ExportDocumentDetailUpdateDto> Details { get; set; } = new List<ExportDocumentDetailUpdateDto>();
    }

    /// <summary>
    /// DTO dòng mặt hàng khi cập nhật Phiếu Xuất dùng nội bộ PENDING.
    /// </summary>
    public class ExportDocumentDetailUpdateDto
    {
        public long? Id { get; set; }

        [Required(ErrorMessage = "BInventoryId không được để trống.")]
        public long BInventoryId { get; set; }

        public long? UnitConversionId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng xuất phải lớn hơn 0.")]
        public decimal Quantity { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }
}
