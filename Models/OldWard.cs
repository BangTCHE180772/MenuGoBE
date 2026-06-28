using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("OldWards")]
public class OldWard
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public long OldDistrictId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("OldDistrictId")]
    public OldDistrict OldDistrict { get; set; } = null!;

    public ICollection<Address> AddressesAsOld { get; set; } = new List<Address>();
}
