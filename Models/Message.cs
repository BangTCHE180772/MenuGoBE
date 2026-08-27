using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Messages")]
public class Message
{
    [Key]
    public long Id { get; set; }

    public long ConversationId { get; set; }

    [ForeignKey("ConversationId")]
    public Conversation? Conversation { get; set; }

    public long? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public Customer? Customer { get; set; }

    public long? EmployeeId { get; set; }

    [ForeignKey("EmployeeId")]
    public Account? Employee { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Topic { get; set; }

    [MaxLength(255)]
    public string? Subject { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }
}
