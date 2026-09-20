using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Document
{
    /// <summary>
    /// DTO yêu cầu tạo mới Phiếu Xuất hủy (ExportDelete Document).
    /// </summary>
    public class ExportDeleteDocumentCreateDto
    {
        public long BranchId { get; set; }

        public string? Code { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? Note { get; set; }

        public List<string>? ImageUrls { get; set; }

        [Required(ErrorMessage = "Danh sách chi tiết mặt hàng hủy không được để trống.")]
        public List<ExportDeleteDocumentDetailCreateDto> Details { get; set; } = new List<ExportDeleteDocumentDetailCreateDto>();
    }

    /// <summary>
    /// DTO dòng mặt hàng khi tạo mới Phiếu Xuất hủy.
    /// </summary>
    public class ExportDeleteDocumentDetailCreateDto
    {
        [Required(ErrorMessage = "BInventoryId không được để trống.")]
        public long BInventoryId { get; set; }

        public long? UnitConversionId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng hủy phải lớn hơn 0.")]
        public decimal Quantity { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO yêu cầu cập nhật Phiếu Xuất hủy PENDING.
    /// </summary>
    public class ExportDeleteDocumentUpdateDto
    {
        public string? Code { get; set; }

        public DateTime OrderDate { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }

        public List<string>? ImageUrls { get; set; }

        [Required(ErrorMessage = "Danh sách chi tiết mặt hàng hủy không được để trống.")]
        public List<ExportDeleteDocumentDetailUpdateDto> Details { get; set; } = new List<ExportDeleteDocumentDetailUpdateDto>();
    }

    /// <summary>
    /// DTO dòng mặt hàng khi cập nhật Phiếu Xuất hủy PENDING.
    /// </summary>
    public class ExportDeleteDocumentDetailUpdateDto
    {
        public long? Id { get; set; }

        [Required(ErrorMessage = "BInventoryId không được để trống.")]
        public long BInventoryId { get; set; }

        public long? UnitConversionId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng hủy phải lớn hơn 0.")]
        public decimal Quantity { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }
}

