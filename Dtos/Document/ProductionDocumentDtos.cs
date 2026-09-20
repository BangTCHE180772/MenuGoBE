using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Document
{
    /// <summary>
    /// DTO yêu cầu tạo mới Phiếu Sản xuất (Production Document).
    /// </summary>
    public class ProductionDocumentCreateDto
    {
        public long BranchId { get; set; }

        public string? Code { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Danh sách chi tiết mặt hàng sản xuất không được để trống.")]
        public List<ProductionDocumentDetailCreateDto> Details { get; set; } = new List<ProductionDocumentDetailCreateDto>();
    }

    /// <summary>
    /// DTO dòng sản phẩm sản xuất (thành phẩm hoặc nguyên vật liệu) khi tạo mới Phiếu Sản xuất.
    /// </summary>
    public class ProductionDocumentDetailCreateDto
    {
        [Required(ErrorMessage = "BInventoryId không được để trống.")]
        public long BInventoryId { get; set; }

        public long? UnitConversionId { get; set; }
        public long? FatherId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng sản xuất/tiêu hao phải lớn hơn 0.")]
        public decimal Quantity { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }

        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        [MaxLength(100)]
        public string? BatchCode { get; set; }
    }

    /// <summary>
    /// DTO yêu cầu cập nhật Phiếu Sản xuất PENDING.
    /// </summary>
    public class ProductionDocumentUpdateDto
    {
        public string? Code { get; set; }

        public DateTime OrderDate { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Danh sách chi tiết mặt hàng sản xuất không được để trống.")]
        public List<ProductionDocumentDetailUpdateDto> Details { get; set; } = new List<ProductionDocumentDetailUpdateDto>();
    }

    /// <summary>
    /// DTO dòng sản phẩm sản xuất khi cập nhật Phiếu Sản xuất PENDING.
    /// </summary>
    public class ProductionDocumentDetailUpdateDto
    {
        public long? Id { get; set; }

        [Required(ErrorMessage = "BInventoryId không được để trống.")]
        public long BInventoryId { get; set; }

        public long? UnitConversionId { get; set; }
        public long? FatherId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng sản xuất/tiêu hao phải lớn hơn 0.")]
        public decimal Quantity { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }

        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        [MaxLength(100)]
        public string? BatchCode { get; set; }
    }
}

