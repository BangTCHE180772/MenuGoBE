using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Document
{
    public class ProductionDocumentCreateDto
    {
        [Required]
        public long BranchId { get; set; }

        [Required]
        public DateTime BusinessDate { get; set; }

        /// <summary>
        /// Bắt buộc: Id tài khoản người tạo phiếu (truyền từ Frontend).
        /// </summary>
        [Required]
        public long CreatedByAccountId { get; set; }

        [Required]
        public long FinishedBInventoryId { get; set; }


        [Required]
        [Range(0.001, 10000000000.000, ErrorMessage = "Số lượng thành phẩm sản xuất phải từ 0,001 đến 10.000.000.000 (10 tỷ).")]
        public decimal FinishedQuantity { get; set; }

        public string? Note { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 nguyên vật liệu cấu thành.")]
        public List<ProductionRawMaterialDetailDto> RawMaterials { get; set; } = new List<ProductionRawMaterialDetailDto>();
    }

    public class ProductionRawMaterialDetailDto
    {
        [Required]
        public long BInventoryId { get; set; }

        [Required]
        [Range(0.001, 10000000000.000, ErrorMessage = "Số lượng nguyên vật liệu phải từ 0,001 đến 10.000.000.000 (10 tỷ).")]
        public decimal Quantity { get; set; }

        public string? Note { get; set; }
    }


}
