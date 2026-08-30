using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models
{
    #region Thực thể Phân Bổ Lô Hàng (BatchAllocation)
    /// <summary>
    /// Lưu trữ chi tiết việc cấp phát / tiêu hao Lô hàng cho từng dòng chi tiết chứng từ (DocumentDetail).
    /// Phục vụ việc truy xuất nguồn gốc Lô (Batch Traceability) và hoàn trả Lô.
    /// </summary>
    [Table("BatchAllocations")]
    public class BatchAllocation
    {
        #region Khóa chính
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        #endregion

        #region Khóa ngoại Chi tiết Chứng từ
        [Required]
        public long DocumentDetailId { get; set; }

        [ForeignKey("DocumentDetailId")]
        public virtual DocumentDetail DocumentDetail { get; set; } = null!;
        #endregion

        #region Khóa ngoại Lô Hàng
        [Required]
        public long BatchId { get; set; }

        [ForeignKey("BatchId")]
        public virtual BInventoryBatch Batch { get; set; } = null!;
        #endregion

        #region Chi tiết Phân bổ
        [Required]
        public BatchAllocationType AllocationType { get; set; }

        /// <summary>
        /// Số lượng (theo đơn vị cơ sở) được phân bổ từ Lô này.
        /// </summary>
        [Column(TypeName = "decimal(51,3)")]
        public decimal QuantityAllocated { get; set; }

        /// <summary>
        /// Giá vốn riêng của Lô tại thời điểm phân bổ.
        /// </summary>
        [Column(TypeName = "decimal(108,12)")]
        public decimal UnitCost { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        #endregion
    }
    #endregion
}
