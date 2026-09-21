using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models
{
    #region Thực thể Lô Hàng Kho Chi Nhánh (BInventoryBatch)
    /// <summary>
    /// Lưu trữ thông tin từng Lô hàng vật lý của một mặt hàng tại chi nhánh.
    /// Đảm bảo tính toán hạn sử dụng, chi phí lô riêng và phục vụ thuật toán FEFO.
    /// </summary>
    [Table("BInventoryBatches")]
    public class BInventoryBatch
    {
        #region Khóa chính
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        #endregion

        #region Khóa ngoại Kho Chi Nhánh
        [Required]
        public long BInventoryId { get; set; }

        [ForeignKey("BInventoryId")]
        public virtual BInventory BInventory { get; set; } = null!;
        #endregion

        #region Thông tin Lô & Hạn sử dụng
        /// <summary>
        /// Mã lô hàng (duy nhất trong phạm vi BInventoryId).
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string BatchCode { get; set; } = string.Empty;

        /// <summary>
        /// Số lượng ban đầu khi nhập/sinh Lô.
        /// </summary>
        [Column(TypeName = "decimal(51,3)")]
        public decimal QuantityOriginal { get; set; }

        /// <summary>
        /// Số lượng tồn thực tế còn lại của Lô.
        /// </summary>
        [Column(TypeName = "decimal(51,3)")]
        public decimal QuantityRemaining { get; set; }

        /// <summary>
        /// Giá vốn riêng của Lô hàng (Lot-level actual cost).
        /// </summary>
        [Column(TypeName = "decimal(108,12)")]
        public decimal UnitCost { get; set; }

        /// <summary>
        /// Ngày sản xuất (UTC).
        /// </summary>
        public DateTime? ManufactureDate { get; set; }

        /// <summary>
        /// Ngày hết hạn (UTC).
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Ngày nhận hàng / nhập kho (UTC).
        /// </summary>
        public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
        #endregion

        #region Truy xuất nguồn gốc chuyển kho
        /// <summary>
        /// ID của Lô nguồn (khi nhận chuyển kho từ chi nhánh khác).
        /// </summary>
        public long? SourceBatchId { get; set; }

        [ForeignKey("SourceBatchId")]
        public virtual BInventoryBatch? SourceBatch { get; set; }

        public virtual ICollection<BInventoryBatch> DerivedBatches { get; set; } = new List<BInventoryBatch>();
        #endregion

        #region Trạng thái & Audit
        [Required]
        public BatchStatus Status { get; set; } = BatchStatus.Active;

        /// <summary>
        /// Cờ tắt thông báo cảnh báo hạn sử dụng đối với Lô này.
        /// </summary>
        public bool IsNotificationMuted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Navigation Phân bổ
        public virtual ICollection<BatchAllocation> BatchAllocations { get; set; } = new List<BatchAllocation>();
        #endregion
    }
    #endregion
}
