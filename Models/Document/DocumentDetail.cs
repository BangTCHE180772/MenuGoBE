using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("DocumentDetails")]
public class DocumentDetail
{
    [Key]
    public long Id { get; set; }

    public long DocumentId { get; set; }
    public long BInventoryId { get; set; }
    public long? FatherId { get; set; } // Liên kết Dòng gốc (Trả hàng) hoặc Cây NVL-Thành phẩm (Sản xuất)
    public long? UnitConversionId { get; set; }

    // ==========================================
    // 📸 SNAPSHOT MẶT HÀNG & ĐƠN VỊ TÍNH
    // ==========================================
    [MaxLength(255)] public string SnapshotProductName { get; set; } = string.Empty;
    [MaxLength(100)] public string SnapshotProductCode { get; set; } = string.Empty;
    [MaxLength(100)] public string SnapshotUnitName { get; set; } = string.Empty; // Đơn vị giao dịch (thùng/chai)
    [MaxLength(100)] public string SnapshotBaseUnitName { get; set; } = string.Empty; // Đơn vị kho cơ sở

    // ==========================================
    // 📸 SNAPSHOT SỐ LƯỢNG & GIÁ TRỊ (KHI POST)
    // ==========================================
    [Column(TypeName = "decimal(18,6)")] 
    public decimal ConversionRate { get; set; } = 1; // Tỷ lệ quy đổi chốt cố định

    [Column(TypeName = "decimal(51,3)")] 
    public decimal Quantity { get; set; } // Số lượng theo đơn vị chọn

    [Column(TypeName = "decimal(51,3)")] 
    public decimal BaseQuantity { get; set; } // Số lượng kho cơ sở = Quantity * ConversionRate

    [Column(TypeName = "decimal(108,12)")] 
    public decimal UnitPrice { get; set; } // Đơn giá mua/bán thỏa thuận

    /// <summary>
    /// SNAPSHOT Giá vốn bình quân (Avg Cost) tại đúng mốc PostedAt.
    /// Căn cứ tính giá vốn cho Xuất hủy, Trả hàng, Sản xuất, Kiểm kho.
    /// </summary>
    [Column(TypeName = "decimal(108,12)")] 
    public decimal SnapshotAvgCost { get; set; }

    // --- SNAPSHOT CHO NGHIỆP VỤ ĐẶC THÙ ---
    [Column(TypeName = "decimal(51,3)")] public decimal? SystemQuantity { get; set; } // [Kiểm kho] Tồn hệ thống lúc post
    [Column(TypeName = "decimal(51,3)")] public decimal? ActualQuantity { get; set; } // [Kiểm kho] Tồn thực tế

    [Column(TypeName = "decimal(108,12)")] public decimal? AdjustedCostDelta { get; set; } // [Điều chỉnh GV] Chênh lệch giá trị kho (+/-)
    [Column(TypeName = "decimal(108,12)")] public decimal? NewAvgCost { get; set; } // [Điều chỉnh GV] Giá vốn mới áp dụng
    [Column(TypeName = "decimal(51,3)")] public decimal? ReceivedQuantity { get; set; } // [Chuyển kho] Số lượng thực nhận

    // --- SNAPSHOT CHO LÔ HÀNG (BATCH) ---
    [MaxLength(100)] public string? BatchCodeSnapshot { get; set; }
    public DateTime? ManufactureDateSnapshot { get; set; }
    public DateTime? ExpiryDateSnapshot { get; set; }

    public bool IsDeleted { get; set; } = false;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("DocumentId")] public Document Document { get; set; } = null!;
    [ForeignKey("BInventoryId")] public BInventory BInventory { get; set; } = null!;
    [ForeignKey("FatherId")] public DocumentDetail? Father { get; set; }

    [InverseProperty("Father")] public ICollection<DocumentDetail> Children { get; set; } = new List<DocumentDetail>();
    [ForeignKey("UnitConversionId")] public UnitConversion? UnitConversion { get; set; }
    public virtual ICollection<BatchAllocation> BatchAllocations { get; set; } = new List<BatchAllocation>();
}
