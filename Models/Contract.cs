using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Contracts")]
public class Contract
{
    [Key]
    public long Id { get; set; }

    public long AccountId { get; set; }

    public long RoleId { get; set; }

    public long BranchId { get; set; }

    public string Type { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public string SalaryType { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,4)")]
    public decimal BaseSalary { get; set; }

    public int? BaseWorkDay { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("AccountId")]
    public Account Account { get; set; } = null!;

    [ForeignKey("RoleId")]
    public Role Role { get; set; } = null!;

    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;
}
