using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("ShiftRoleRequirements")]
public class ShiftRoleRequirement
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long ShiftId { get; set; }

    [Required]
    public long RoleId { get; set; }

    [Required]
    public int MinQuantity { get; set; } = 1;

    // Navigation
    [ForeignKey("ShiftId")]
    public Shift Shift { get; set; } = null!;

    [ForeignKey("RoleId")]
    public Role Role { get; set; } = null!;
}
