using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

// "Đổi ca / làm ca khác" — nhân viên yêu cầu đổi giờ ca của chính mình vào
// một ngày cụ thể (khác với ShiftSwap là chuyển ca cho người khác).
[Table("ShiftChangeRequests")]
public class ShiftChangeRequest
{
    [Key]
    public long Id { get; set; }

    public long WorkScheduleId { get; set; }

    public long AccountId { get; set; }

    public long BranchId { get; set; }

    public DateOnly WorkDate { get; set; }

    // Snapshot ca cũ + trạng thái cũ của WorkSchedule (để khôi phục nếu bị từ chối)
    public long OldShiftId { get; set; }
    public TimeOnly OldStartTime { get; set; }
    public TimeOnly OldEndTime { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public bool OldManagerApproved { get; set; }

    // Ca mới được yêu cầu (luôn resolve về 1 Shift thật, kể cả khi nhập giờ tùy chỉnh)
    public long NewShiftId { get; set; }
    public TimeOnly NewStartTime { get; set; }
    public TimeOnly NewEndTime { get; set; }
    public bool IsNightShift { get; set; }

    [MaxLength(255)]
    public string? Reason { get; set; }

    public ShiftChangeRequestStatus Status { get; set; } = ShiftChangeRequestStatus.Pending;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public long? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    [MaxLength(255)]
    public string? RejectReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("WorkScheduleId")]
    public WorkSchedule WorkSchedule { get; set; } = null!;

    [ForeignKey("AccountId")]
    public Account Account { get; set; } = null!;

    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("OldShiftId")]
    public Shift OldShift { get; set; } = null!;

    [ForeignKey("NewShiftId")]
    public Shift NewShift { get; set; } = null!;

    [ForeignKey("ApprovedBy")]
    public Account? Approver { get; set; }
}
