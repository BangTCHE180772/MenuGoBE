using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("OldDistricts")]
public class OldDistrict
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public long OldProvinceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("OldProvinceId")]
    public OldProvince OldProvince { get; set; } = null!;

    public ICollection<OldWard> OldWards { get; set; } = new List<OldWard>();
}
