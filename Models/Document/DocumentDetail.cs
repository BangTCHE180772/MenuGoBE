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

    /// <summary>
    /// Self-reference: Id của DocumentDetail cha (null nếu là bản ghi gốc)
    /// Quan hệ: 1 DocumentDetail cha có thể có nhiều DocumentDetail con
    /// </summary>
    public long? FatherId { get; set; }

    /// <summary>
    /// Quan hệ 1-1 với UnitConversion (null nếu không quy đổi đơn vị)
    /// </summary>
    public long? UnitConversionId { get; set; }

    [Column(TypeName = "decimal(51,3)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(51,3)")]
    public decimal? SystemQuantity { get; set; }

    [Column(TypeName = "decimal(108,12)")]
    public decimal UnitPrice { get; set; }

    public bool IsDeleted { get; set; } = false;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("DocumentId")]
    public Document Document { get; set; } = null!;

    [ForeignKey("BInventoryId")]
    public BInventory BInventory { get; set; } = null!;

    /// <summary>
    /// DocumentDetail cha (self-reference)
    /// </summary>
    [ForeignKey("FatherId")]
    public DocumentDetail? Father { get; set; }

    /// <summary>
    /// Danh sách các DocumentDetail con
    /// </summary>
    [InverseProperty("Father")]
    public ICollection<DocumentDetail> Children { get; set; } = new List<DocumentDetail>();

    /// <summary>
    /// Quan hệ 1-1: quy đổi đơn vị áp dụng cho dòng này
    /// </summary>
    [ForeignKey("UnitConversionId")]
    public UnitConversion? UnitConversion { get; set; }

    public ICollection<InventoryLedger> InventoryLedgers { get; set; } = new List<InventoryLedger>();
}
