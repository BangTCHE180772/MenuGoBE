using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Items")]
public class Item
{
    [Key]
    public long Id { get; set; }

    public long CategoryId { get; set; }

    public long ChainId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("CategoryId")]
    public Category Category { get; set; } = null!;

    [ForeignKey("ChainId")]
    public Chain Chain { get; set; } = null!;

    public ICollection<Variant> Variants { get; set; } = new List<Variant>();
}
