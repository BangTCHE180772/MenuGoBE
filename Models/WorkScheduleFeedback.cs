using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("WorkScheduleFeedbacks")]
public class WorkScheduleFeedback
{
    [Key]
    public long Id { get; set; }

    public long WorkScheduleId { get; set; }

    public long AccountId { get; set; }

    public long BranchId { get; set; }

    [MaxLength(100)]
    public string FeedbackType { get; set; } = "OTDispute";

    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    public ShiftFeedbackStatus Status { get; set; } = ShiftFeedbackStatus.Pending;

    [MaxLength(1000)]
    public string? ResponseMessage { get; set; }

    public long? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("WorkScheduleId")]
    public WorkSchedule WorkSchedule { get; set; } = null!;

    [ForeignKey("AccountId")]
    public Account Account { get; set; } = null!;

    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("ResolvedBy")]
    public Account? Resolver { get; set; }
}
