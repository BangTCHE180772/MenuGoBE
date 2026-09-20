using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("Documents")]
public class Document
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }
    public long? ToBranchId { get; set; }
    public long? PartnerId { get; set; }
    public long? ParentDocumentId { get; set; }

    /// <summary>
    /// Liên kết trực tiếp với Order (dùng cho Sale/CustomerReturn documents)
    /// </summary>
    public long? OrderId { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long PostingSequence { get; set; }

    /// <summary>
    /// Ngày chứng từ/hóa đơn (User chọn - Chỉ hiển thị, không dùng tính toán sổ kho)
    /// </summary>
    public DateTime OrderDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    public DocumentType Type { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public DocumentTransferStatus TransferStatus { get; set; }

    // ==========================================
    // 📸 SNAPSHOT MASTER DATA (THÔNG TIN CHỨNG TỪ)
    // ==========================================
    [MaxLength(255)] public string SnapshotBranchName { get; set; } = string.Empty;
    [MaxLength(255)] public string? SnapshotToBranchName { get; set; }
    [MaxLength(255)] public string? SnapshotPartnerName { get; set; }

    // Audit Users Snapshot
    public long? CreatedBy { get; set; }
    [MaxLength(255)] public string? SnapshotCreatedByName { get; set; }
    [MaxLength(100)] public string? SnapshotCreatedByUsername { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public long? PostedBy { get; set; }
    [MaxLength(255)] public string? SnapshotPostedByName { get; set; }
    [MaxLength(100)] public string? SnapshotPostedByUsername { get; set; }
    
    /// <summary>
    /// SystemDate: Mốc thời gian BẤM CHỐT PHIẾU thực tế.
    /// Tất cả tính toán kho, giá vốn và báo cáo ĐỀU CĂN CỨ THEO MỐC NÀY.
    /// </summary>
    public DateTime? PostedAt { get; set; }

    public long? DeletedBy { get; set; }
    [MaxLength(255)] public string? SnapshotDeletedByName { get; set; }
    public DateTime? DeletedAt { get; set; }

    #region --- BRANCH TRANSFER / RECEIVER METADATA ---
    /// <summary>
    /// ID Tài khoản Quản lý / Thủ kho phía Chi nhánh nhận (ToBranch)
    /// Chỉ dùng cho phiếu Transfer hoặc Import nội bộ
    /// </summary>
    public long? ReceiverAccountId { get; set; }

    /// <summary>
    /// Snapshot Tên Quản lý nhận hàng tại thời điểm nhận
    /// </summary>
    [MaxLength(255)] 
    public string? SnapshotReceiverName { get; set; }

    /// <summary>
    /// Snapshot Username Quản lý nhận hàng
    /// </summary>
    [MaxLength(100)] 
    public string? SnapshotReceiverUsername { get; set; }

    /// <summary>
    /// Thời điểm Chi nhánh nhận phản hồi (Chấp nhận / Từ chối / Nhận một phần)
    /// Chỉ dùng cho phiếu Transfer
    /// </summary>
    public DateTime? ResponseDate { get; set; }

    // Navigation Property
    [ForeignKey("ReceiverAccountId")] 
    public Account? ReceiverAccount { get; set; }
    #endregion

    // Financial Totals
    [Column(TypeName = "decimal(108,12)")] public decimal TotalAmount { get; set; }
    [Column(TypeName = "decimal(108,12)")] public decimal? AmountDue { get; set; }
    [Column(TypeName = "decimal(108,12)")] public decimal AmountPaid { get; set; } = 0;

    [MaxLength(255)] public string? Note { get; set; }
    [MaxLength(255)] public string? DeleteNote { get; set; }

    /// <summary>
    /// Danh sách URL hình ảnh chứng từ/biên lai/bằng chứng nhận hàng (Lưu JSON string)
    /// </summary>
    public string? ImageUrls { get; set; }

    // Navigation
    [ForeignKey("BranchId")] public Branch Branch { get; set; } = null!;
    [ForeignKey("ToBranchId")] public Branch? ToBranch { get; set; }
    [ForeignKey("PartnerId")] public Partner? Partner { get; set; }
    [ForeignKey("ParentDocumentId")] public Document? ParentDocument { get; set; }
    [ForeignKey("CreatedBy")] public Account? Creator { get; set; }
    [ForeignKey("PostedBy")] public Account? Poster { get; set; }
    [ForeignKey("DeletedBy")] public Account? Deleter { get; set; }

    [InverseProperty("ParentDocument")] public ICollection<Document> ChildDocuments { get; set; } = new List<Document>();
    public ICollection<DocumentDetail> DocumentDetails { get; set; } = new List<DocumentDetail>();
    public ICollection<CashFlow> CashFlows { get; set; } = new List<CashFlow>();
    public ICollection<InventoryLedger> InventoryLedgers { get; set; } = new List<InventoryLedger>();
}
