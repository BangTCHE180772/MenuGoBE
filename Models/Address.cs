using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Addresses")]
public class Address
{
    [Key]
    public long Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public long? NewWardId { get; set; }

    public long? OldWardId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("NewWardId")]
    public NewWard? NewWard { get; set; }

    [ForeignKey("OldWardId")]
    public OldWard? OldWard { get; set; }
}
