using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("GuestChatSessions")]
public class GuestChatSession
{
    [Key]
    public long Id { get; set; }

    [Required]
    public Guid GuestId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string GuestName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Token { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastActiveTime { get; set; } = DateTime.UtcNow;

    public long? BranchId { get; set; }

    [ForeignKey("BranchId")]
    public Branch? Branch { get; set; }

    // Navigation
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
