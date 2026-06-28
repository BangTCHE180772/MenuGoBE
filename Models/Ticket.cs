using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Tickets")]
public class Ticket
{
    [Key]
    public long Id { get; set; }

    public long AccountId { get; set; }

    public long BranchId { get; set; }

    public string Reason { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,4)")]
    public decimal PenaltyAmount { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("AccountId")]
    public Account Account { get; set; } = null!;

    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;
}
