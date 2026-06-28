using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Areas")]
public class Area
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    public ICollection<Table> Tables { get; set; } = new List<Table>();
}
