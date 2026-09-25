using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Document
{
    /// <summary>
    /// DTO yêu cầu tạo mới Phiếu Kiểm kho (Inventory Check Document).
    /// </summary>
    public class CheckDocumentCreateDto
    {
        public long BranchId { get; set; }

        public string? Code { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Danh sách chi tiết mặt hàng kiểm kho không được để trống.")]
        public List<CheckDocumentDetailCreateDto> Details { get; set; } = new List<CheckDocumentDetailCreateDto>();
    }

    /// <summary>
    /// DTO dòng kiểm kho cho từng mặt hàng khi tạo mới Phiếu Kiểm kho.
    /// </summary>
    public class CheckDocumentDetailCreateDto
    {
        [Required(ErrorMessage = "BInventoryId không được để trống.")]
        public long BInventoryId { get; set; }

        public long? UnitConversionId { get; set; }

        public long? BatchId { get; set; }

        public string? BatchCodeSnapshot { get; set; }

        public DateTime? ManufactureDateSnapshot { get; set; }

        public DateTime? ExpiryDateSnapshot { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số lượng tồn hệ thống không được nhỏ hơn 0.")]
        public decimal? SystemQuantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số lượng tồn thực tế không được nhỏ hơn 0.")]
        public decimal ActualQuantity { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO yêu cầu cập nhật Phiếu Kiểm kho PENDING.
    /// </summary>
    public class CheckDocumentUpdateDto
    {
        public string? Code { get; set; }

        public DateTime OrderDate { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Danh sách chi tiết mặt hàng kiểm kho không được để trống.")]
        public List<CheckDocumentDetailUpdateDto> Details { get; set; } = new List<CheckDocumentDetailUpdateDto>();
    }

    /// <summary>
    /// DTO dòng kiểm kho khi cập nhật Phiếu Kiểm kho PENDING.
    /// </summary>
    public class CheckDocumentDetailUpdateDto
    {
        public long? Id { get; set; }

        [Required(ErrorMessage = "BInventoryId không được để trống.")]
        public long BInventoryId { get; set; }

        public long? UnitConversionId { get; set; }

        public long? BatchId { get; set; }

        public string? BatchCodeSnapshot { get; set; }

        public DateTime? ManufactureDateSnapshot { get; set; }

        public DateTime? ExpiryDateSnapshot { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số lượng tồn hệ thống không được nhỏ hơn 0.")]
        public decimal? SystemQuantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số lượng tồn thực tế không được nhỏ hơn 0.")]
        public decimal ActualQuantity { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }
}

