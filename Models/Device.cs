using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Devices")]
public class Device
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string DeviceType { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public long SetupBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastActiveAt { get; set; }

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("SetupBy")]
    public Account SetupByAccount { get; set; } = null!;
}
