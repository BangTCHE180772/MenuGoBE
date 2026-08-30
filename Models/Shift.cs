using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Shifts")]
public class Shift
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsNightShift { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal NightBonusRate { get; set; } = 1.3m; // Multiplier e.g. 1.3 for +30%

    [Column(TypeName = "decimal(18,4)")]
    public decimal NightAllowance { get; set; } = 0m; // Flat allowance e.g. 50,000 VND

    public TimeOnly? NightStartTime { get; set; }

    public TimeOnly? NightEndTime { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal NightHours { get; set; } = 0m; // Number of hours considered night shift

    [Column(TypeName = "decimal(18,4)")]
    public decimal StandardHours { get; set; } = 8.0m; // Standard shift hours

    public bool IsActive { get; set; }

    public string ApplicableTo { get; set; } = "ALL"; // "ALL", "FULL_TIME", "PART_TIME"

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<WorkSchedule> WorkSchedules { get; set; } = new List<WorkSchedule>();
    public ICollection<ShiftRoleRequirement> RoleRequirements { get; set; } = new List<ShiftRoleRequirement>();
}

