using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Chains")]
public class Chain
{
    [Key]
    public long Id { get; set; }

    public TimeOnly CloseTime { get; set; }

    public TimeOnly OpenTime { get; set; }

    public string BackgroundImage { get; set; } = string.Empty;

    public string LogoImage { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public long AddressId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("AddressId")]
    public Address Address { get; set; } = null!;

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
