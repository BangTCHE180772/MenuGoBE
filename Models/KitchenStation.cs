using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("KitchenStations")]
public class KitchenStation
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
