using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Role")]
public class Role
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<TempRole> TempRoles { get; set; } = new List<TempRole>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<ShiftRoleRequirement> ShiftRoleRequirements { get; set; } = new List<ShiftRoleRequirement>();
}

