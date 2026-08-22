using System;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Chat;

public class ConversationCreateDto
{
    [Required(ErrorMessage = "Chi nhánh là bắt buộc.")]
    public long BranchId { get; set; }

    [Required(ErrorMessage = "Chủ đề là bắt buộc.")]
    public string Topic { get; set; } = "General"; // General, Booking, Menu, Event, Pricing, Allergy, Other

    [MaxLength(255)]
    public string? Subject { get; set; }

    [MaxLength(50)]
    public string? PreferredContactMethod { get; set; } // Chat, Phone, Email

    [Required(ErrorMessage = "Tin nhắn đầu tiên là bắt buộc.")]
    public string FirstMessageContent { get; set; } = string.Empty;
}

public class MessageCreateDto
{
    [Required(ErrorMessage = "Nội dung tin nhắn là bắt buộc.")]
    public string Content { get; set; } = string.Empty;
}

public class ConversationViewDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public long BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string Topic { get; set; } = "General";
    public string? Subject { get; set; }
    public string? PreferredContactMethod { get; set; }
    public long? AssignedEmployeeId { get; set; }
    public string? AssignedEmployeeName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int UnreadCount { get; set; }
}

public class MessageViewDto
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public long? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public long? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class ConversationAssignmentHistoryViewDto
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public long? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public string? CreatorName { get; set; }
}
