using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Images")]
public class Image
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string ImageLink { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
