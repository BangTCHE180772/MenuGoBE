using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("ConversationAssignmentHistories")]
public class ConversationAssignmentHistory
{
    [Key]
    public long Id { get; set; }

    public long ConversationId { get; set; }

    [ForeignKey("ConversationId")]
    public Conversation? Conversation { get; set; }

    public long? EmployeeId { get; set; }

    [ForeignKey("EmployeeId")]
    public Account? Employee { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // Assigned, Reassigned, Unassigned

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public long CreatedBy { get; set; } // AccountId of who performed the action

    [ForeignKey("CreatedBy")]
    public Account? Creator { get; set; }
}
