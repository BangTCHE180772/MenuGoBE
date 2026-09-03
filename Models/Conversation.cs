using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Conversations")]
public class Conversation
{
    [Key]
    public long Id { get; set; }

    public long? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public Customer? Customer { get; set; }

    public long? GuestChatSessionId { get; set; }

    [ForeignKey("GuestChatSessionId")]
    public GuestChatSession? GuestChatSession { get; set; }

    public long BranchId { get; set; }

    [ForeignKey("BranchId")]
    public Branch? Branch { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Open"; // Open, Assigned, Closed

    [Required]
    [MaxLength(50)]
    public string Topic { get; set; } = "General"; // General, Booking, Menu, Event, Pricing, Allergy, Other

    [MaxLength(255)]
    public string? Subject { get; set; }

    [MaxLength(50)]
    public string? PreferredContactMethod { get; set; } // Chat, Phone, Email

    public long? AssignedEmployeeId { get; set; }

    [ForeignKey("AssignedEmployeeId")]
    public Account? AssignedEmployee { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<ConversationAssignmentHistory> AssignmentHistories { get; set; } = new List<ConversationAssignmentHistory>();
}
