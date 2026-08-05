using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Tables")]
public class Table
{
    [Key]
    public long Id { get; set; }

    public long AreaId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = "Empty";

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("AreaId")]
    public Area Area { get; set; } = null!;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
