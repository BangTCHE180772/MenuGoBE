using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.WorkSchedule;

public class ShiftFeedbackCreateDto
{
    [Required]
    public long WorkScheduleId { get; set; }

    [MaxLength(100)]
    public string FeedbackType { get; set; } = "OTDispute";

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;
}

public class ShiftFeedbackProcessDto
{
    [Required]
    public string Status { get; set; } = "Resolved"; // Resolved | Rejected

    [MaxLength(1000)]
    public string? ResponseMessage { get; set; }

    // Option to update OT directly during resolution
    public bool UpdateOT { get; set; } = false;

    public decimal? ApprovedOTHours { get; set; }

    public string? OTStatus { get; set; } // Approved | Rejected | Pending | None

    public string? OTNotes { get; set; }
}

public class ShiftFeedbackViewDto
{
    public long Id { get; set; }
    public long WorkScheduleId { get; set; }
    public long AccountId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public long BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    
    // Work schedule details
    public DateOnly WorkDate { get; set; }
    public long ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public string ShiftTime { get; set; } = string.Empty;
    public DateTime? CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public decimal? ActualHours { get; set; }
    public decimal ApprovedOTHours { get; set; }
    public string OTStatus { get; set; } = "None";

    // Feedback details
    public string FeedbackType { get; set; } = "OTDispute";
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? ResponseMessage { get; set; }
    public long? ResolvedBy { get; set; }
    public string? ResolverName { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
