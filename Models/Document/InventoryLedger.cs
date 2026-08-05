using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("InventoryLedgers")]
public class InventoryLedger
{
    [Key]
    public long Id { get; set; }

    public long BInventoryId { get; set; }
    public long? DocumentId { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long PostingSequence { get; set; } // Số thứ tự tuyến tính 1 chiều tuyệt đối

    /// <summary>
    /// SystemDate khi bấm chốt phiếu (= Document.PostedAt).
    /// </summary>
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;

    public DocumentType DocumentType { get; set; }

    // ==========================================
    // 📸 SNAPSHOT CONTEXT (NHẬT KÝ KHÔNG THỂ SỬA/XÓA)
    // ==========================================
    public long PostedBy { get; set; }
    [MaxLength(255)] public string SnapshotPostedByName { get; set; } = string.Empty;
    [MaxLength(255)] public string SnapshotBranchName { get; set; } = string.Empty;
    [MaxLength(255)] public string SnapshotProductName { get; set; } = string.Empty;
    [MaxLength(100)] public string SnapshotUnitName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,6)")]
    public decimal ConversionRateSnapshot { get; set; } = 1;

    // ==========================================
    // 📊 BIẾN ĐỘNG & TỔN KHO TỨC THÌ (RUNNING TOTALS)
    // ==========================================
    [Column(TypeName = "decimal(51,3)")] 
    public decimal QuantityDelta { get; set; } // Tăng/giảm tồn kho (+/-) hoặc =0 nếu Điều chỉnh GV

    [Column(TypeName = "decimal(108,12)")] 
    public decimal InventoryValueDelta { get; set; } // Tăng/giảm giá trị kho (+/-)

    [Column(TypeName = "decimal(51,3)")] 
    public decimal RunningQuantity { get; set; } // Tồn kho tức thì ngay sau giao dịch này

    [Column(TypeName = "decimal(108,12)")] 
    public decimal RunningInventoryValue { get; set; } // Tổng giá trị kho tức thì

    [Column(TypeName = "decimal(108,12)")] 
    public decimal RunningAverageCost { get; set; } // Giá vốn bình quân tức thì = RunningInventoryValue / RunningQuantity

    [Column(TypeName = "decimal(108,12)")] 
    public decimal UnitCost { get; set; } // Giá vốn ghi nhận của riêng giao dịch này

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BInventoryId")] public BInventory BInventory { get; set; } = null!;
    [ForeignKey("DocumentId")] public Document? Document { get; set; }
}
