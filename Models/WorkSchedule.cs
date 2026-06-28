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

    public string Status { get; set; } = string.Empty;

    public bool ManagerApproved { get; set; }

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
