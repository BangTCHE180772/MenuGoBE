using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Document
{
    /// <summary>
    /// DTO yêu cầu tạo mới Phiếu Điều chỉnh giá vốn (Cost Adjustment Document).
    /// </summary>
    public class CostAdjustmentDocumentCreateDto
    {
        [Required(ErrorMessage = "Mã chi nhánh không được để trống.")]
        public long BranchId { get; set; }

        public string? Code { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Danh sách chi tiết điều chỉnh giá vốn không được để trống.")]
        public List<CostAdjustmentDocumentDetailCreateDto> Details { get; set; } = new List<CostAdjustmentDocumentDetailCreateDto>();
    }

    /// <summary>
    /// DTO dòng điều chỉnh giá vốn từng mặt hàng khi tạo mới.
    /// </summary>
    public class CostAdjustmentDocumentDetailCreateDto
    {
        [Required(ErrorMessage = "BInventoryId không được để trống.")]
        public long BInventoryId { get; set; }

        public long? UnitConversionId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá vốn mới không được nhỏ hơn 0.")]
        public decimal NewAvgCost { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO yêu cầu cập nhật Phiếu Điều chỉnh giá vốn PENDING.
    /// </summary>
    public class CostAdjustmentDocumentUpdateDto
    {
        public string? Code { get; set; }

        public DateTime OrderDate { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Danh sách chi tiết điều chỉnh giá vốn không được để trống.")]
        public List<CostAdjustmentDocumentDetailUpdateDto> Details { get; set; } = new List<CostAdjustmentDocumentDetailUpdateDto>();
    }

    /// <summary>
    /// DTO dòng điều chỉnh giá vốn khi cập nhật Phiếu PENDING.
    /// </summary>
    public class CostAdjustmentDocumentDetailUpdateDto
    {
        public long? Id { get; set; }

        [Required(ErrorMessage = "BInventoryId không được để trống.")]
        public long BInventoryId { get; set; }

        public long? UnitConversionId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá vốn mới không được nhỏ hơn 0.")]
        public decimal NewAvgCost { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }
}
