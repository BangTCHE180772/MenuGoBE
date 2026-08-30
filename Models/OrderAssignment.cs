using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("OrderAssignments")]
public class OrderAssignment
{
    [Key]
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long AccountId { get; set; }

    public long BranchId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời điểm waiter thao tác lần cuối trên order này.
    /// Dùng để detect stale assignment.
    /// </summary>
    public DateTime LastActionAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// true = đang phụ trách, false = đã kết thúc.
    /// Không xóa bản ghi để giữ lịch sử đối soát.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Lý do deactivate: "Paid", "Cancelled", "Reassigned", "Merged", "ShiftEnded", "ManualUnassign"
    /// </summary>
    public string? DeactivatedReason { get; set; }

    public DateTime? DeactivatedAt { get; set; }

    // Navigation
    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;

    [ForeignKey("AccountId")]
    public Account Account { get; set; } = null!;

    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;
}
