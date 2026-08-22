using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("HolidayConfigs")]
public class HolidayConfig
{
    [Key]
    public long Id { get; set; }

    public long? BranchId { get; set; } // Legacy single branch id

    public string? BranchIds { get; set; } // Comma-separated ids e.g. "1,2,5". Null/empty = all branches

    [Required]
    public string Name { get; set; } = string.Empty; // e.g. "Tết Nguyên Đán 2026", "Quốc Khánh 2/9"

    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Coefficient { get; set; } = 3.0m; // Default holiday multiplier e.g. 3.0

    public bool IsRecurring { get; set; } = false; // True = lặp lại hàng năm (ví dụ: 2/9, 30/4)

    public bool IsActive { get; set; } = true;

    public long? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BranchId")]
    public Branch? Branch { get; set; }

    [ForeignKey("CreatedBy")]
    public Account? Creator { get; set; }
}
