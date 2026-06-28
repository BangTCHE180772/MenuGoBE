using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Evidences")]
public class Evidence
{
    [Key]
    public long Id { get; set; }

    public long TransportClaimId { get; set; }

    public string Link { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("TransportClaimId")]
    public TransportClaim TransportClaim { get; set; } = null!;
}
