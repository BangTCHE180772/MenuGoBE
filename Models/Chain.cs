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

    public long AddressId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("AddressId")]
    public Address Address { get; set; } = null!;

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
}
