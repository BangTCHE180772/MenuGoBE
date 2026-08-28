using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.HolidayConfig;

public class HolidayConfigCreateDto
{
    public long? BranchId { get; set; }
    public List<long>? BranchIds { get; set; }

    [Required(ErrorMessage = "Tên ngày lễ / Tết là bắt buộc")]
    public string Name { get; set; } = string.Empty;

    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }

    [Range(1.0, 10.0, ErrorMessage = "Hệ số phải từ 1.0 đến 10.0")]
    public decimal Coefficient { get; set; } = 3.0m;

    public bool IsRecurring { get; set; } = false; // Lặp lại hàng năm
    public bool IsActive { get; set; } = true;
    public long? CreatedBy { get; set; }
}

public class HolidayConfigUpdateDto
{
    public long Id { get; set; }
    public long? BranchId { get; set; }
    public List<long>? BranchIds { get; set; }

    [Required(ErrorMessage = "Tên ngày lễ / Tết là bắt buộc")]
    public string Name { get; set; } = string.Empty;

    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }

    [Range(1.0, 10.0, ErrorMessage = "Hệ số phải từ 1.0 đến 10.0")]
    public decimal Coefficient { get; set; } = 3.0m;

    public bool IsRecurring { get; set; } = false; // Lặp lại hàng năm
    public bool IsActive { get; set; } = true;
}

public class HolidayConfigViewDto
{
    public long Id { get; set; }
    public long? BranchId { get; set; }
    public string? BranchName { get; set; }
    public List<long>? BranchIds { get; set; }
    public List<string>? BranchNames { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal Coefficient { get; set; }
    public bool IsRecurring { get; set; }
    public bool IsActive { get; set; }
    public long? CreatedBy { get; set; }
    public string? CreatorName { get; set; }
    public DateTime CreatedAt { get; set; }
}
