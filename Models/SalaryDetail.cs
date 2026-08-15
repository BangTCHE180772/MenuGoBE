using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("SalaryDetails")]
public class SalaryDetail
{
    [Key]
    public long Id { get; set; }

    public long PayrollId { get; set; }

    public long AccountId { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public SalaryAdjustmentType Type { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    public string Note { get; set; } = string.Empty;

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("PayrollId")]
    public Payroll Payroll { get; set; } = null!;

    [ForeignKey("AccountId")]
    [InverseProperty("SalaryDetails")]
    public Account Account { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public Account? Creator { get; set; }
}
