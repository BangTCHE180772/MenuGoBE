using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("BInventories")]
public class BInventory
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    public long ProductId { get; set; }

    public BInventoryType Type { get; set; }

    [Column(TypeName = "decimal(108,12)")]
    public decimal Avg { get; set; }

    [Column(TypeName = "decimal(108,12)")]
    public decimal LeftOver { get; set; }

    [Column(TypeName = "decimal(51,3)")]
    public decimal Quantity { get; set; }



    public bool ChainActive { get; set; }

    public bool BranchActive { get; set; }

    public bool IsManageQuantity { get; set; } = false;

    [Column(TypeName = "decimal(51,3)")]
    public decimal MinStorage { get; set; } = 0;

    [Column(TypeName = "decimal(51,3)")]
    public decimal MaxStorage { get; set; } = 1000000000;

    /// <summary>Ngưỡng cảnh báo tồn kho cấp bách tùy chỉnh riêng cho mặt hàng này (null = theo chi nhánh)</summary>
    [Column(TypeName = "decimal(51,3)")]
    public decimal? CustomCriticalThreshold { get; set; }

    /// <summary>Ngưỡng cảnh báo tồn kho sắp hết tùy chỉnh riêng cho mặt hàng này (null = theo chi nhánh)</summary>
    [Column(TypeName = "decimal(51,3)")]
    public decimal? CustomWarningThreshold { get; set; }

    /// <summary>Cờ bật/tắt cảnh báo tồn kho đối với mặt hàng này</summary>
    public bool IsAlertEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;

    public ICollection<InventoryLedger> InventoryLedgers { get; set; } = new List<InventoryLedger>();
    public ICollection<DocumentDetail> DocumentDetails { get; set; } = new List<DocumentDetail>();
    public ICollection<BInventoryBatch> Batches { get; set; } = new List<BInventoryBatch>();
}
