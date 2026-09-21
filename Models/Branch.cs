using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Branches")]
public class Branch
{
    [Key]
    public long Id { get; set; }

    public long ChainId { get; set; }

    public long AddressId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public TimeOnly CloseTime { get; set; }

    public TimeOnly OpenTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PostingLockDate { get; set; }

    public string Status { get; set; } = "Hoạt động";

    public bool IsDeleted { get; set; } = false;

    // GPS location for check-in validation
    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    /// <summary>Bán kính tính bằng mét cho phép điểm danh (mặc định 100m)</summary>
    public int CheckinRadiusMeters { get; set; } = 100;

    /// <summary>Thời gian lưu trữ tin nhắn của khách ẩn danh (phút, mặc định 43200 phút = 30 ngày)</summary>
    public int AnonymousChatRetentionMinutes { get; set; } = 43200;

    /// <summary>Ngày trả lương hàng tháng của chi nhánh (mặc định ngày 10 hàng tháng)</summary>
    public int PayrollPayday { get; set; } = 10;

    /// <summary>Số ngày ngưỡng cảnh báo hạn sử dụng cấp bách (mặc định 7 ngày, tối thiểu 7 ngày)</summary>
    public int BatchCriticalDays { get; set; } = 7;

    /// <summary>Số ngày ngưỡng cảnh báo hạn sử dụng thông thường (mặc định 14 ngày, tối thiểu 14 ngày)</summary>
    public int BatchWarningDays { get; set; } = 14;

    /// <summary>Ngưỡng cảnh báo tồn kho cấp bách chung của chi nhánh (mặc định 20 đơn vị)</summary>
    [Column(TypeName = "decimal(51,3)")]
    public decimal StockCriticalThreshold { get; set; } = 20;

    /// <summary>Ngưỡng cảnh báo tồn kho sắp hết chung của chi nhánh (mặc định 40 đơn vị)</summary>
    [Column(TypeName = "decimal(51,3)")]
    public decimal StockWarningThreshold { get; set; } = 40;

    // Navigation
    [ForeignKey("ChainId")]
    public Chain Chain { get; set; } = null!;

    [ForeignKey("AddressId")]
    public Address Address { get; set; } = null!;

    public ICollection<Area> Areas { get; set; } = new List<Area>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<WorkSchedule> WorkSchedules { get; set; } = new List<WorkSchedule>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    // 9. Inventory Module
    public ICollection<BInventory> BInventories { get; set; } = new List<BInventory>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Partner> Partners { get; set; } = new List<Partner>();
    public ICollection<CashFlow> CashFlows { get; set; } = new List<CashFlow>();
}
