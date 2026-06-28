using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("NewWards")]
public class NewWard
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public long NewProvinceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("NewProvinceId")]
    public NewProvince NewProvince { get; set; } = null!;

    public ICollection<Address> AddressesAsNew { get; set; } = new List<Address>();
}
