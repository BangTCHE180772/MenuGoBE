using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("WorkSchedules")]
public class WorkSchedule
{
    [Key]
    public long Id { get; set; }

    public long AccountId { get; set; }

    public long BranchId { get; set; }

    public long ShiftId { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    public DateOnly WorkDate { get; set; }

    public DateTime? CheckInAt { get; set; }

    public DateTime? CheckOutAt { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? ActualHours { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal StandardHours { get; set; } = 8.0m;

    [Column(TypeName = "decimal(18,4)")]
    public decimal ApprovedOTHours { get; set; } = 0m;

    public string OTStatus { get; set; } = "None"; // None | Pending | Approved | Rejected

    public bool IsAutoCheckout { get; set; } = false;

    public string OTNotes { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool ManagerApproved { get; set; } = true;

    /// <summary>
    /// Đánh dấu nhân viên này là bếp trưởng phụ trách chính cho ca làm việc này
    /// (ngày + ca + chi nhánh cụ thể). Mỗi ca chỉ có tối đa 1 người giữ vai trò này —
    /// đặt cờ mới sẽ tự động bỏ cờ của người trước đó trong cùng ca.
    /// Thuần hiển thị/theo dõi, chưa gắn thêm quyền hạn nào.
    /// </summary>
    public bool IsHeadChef { get; set; } = false;

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("AccountId")]
    public Account Account { get; set; } = null!;

    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("ShiftId")]
    public Shift Shift { get; set; } = null!;
}
