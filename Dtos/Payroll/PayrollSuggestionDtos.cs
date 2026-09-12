using System.ComponentModel.DataAnnotations;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Payroll;

public class PayrollSuggestionCreateDto
{
    [Required]
    public long BranchId { get; set; }

    [Required]
    public long AccountId { get; set; }

    [Required]
    public int Month { get; set; }

    [Required]
    public int Year { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public SalaryAdjustmentType Type { get; set; } = SalaryAdjustmentType.Allowance;

    [Required]
    [Range(0.01, 1000000000, ErrorMessage = "Số tiền phải lớn hơn 0")]
    public decimal Amount { get; set; }

    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    public string ApplyOption { get; set; } = "NextMonth"; // NextMonth | CurrentMonth | ResignImmediate

    public bool IsResigned { get; set; } = false;
}

public class PayrollSuggestionUpdateDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public SalaryAdjustmentType Type { get; set; } = SalaryAdjustmentType.Allowance;

    [Required]
    [Range(0.01, 1000000000, ErrorMessage = "Số tiền phải lớn hơn 0")]
    public decimal Amount { get; set; }

    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    public string ApplyOption { get; set; } = "NextMonth";

    public bool IsResigned { get; set; } = false;
}

public class PayrollSuggestionProcessDto
{
    [Required]
    public string Status { get; set; } = "Approved"; // Approved | Rejected

    [MaxLength(1000)]
    public string? ManagerNote { get; set; }

    public string? ForceApplyOption { get; set; } // Optional override by manager
}

public class PayrollSuggestionQueryDto
{
    public long? BranchId { get; set; }
    public long? AccountId { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public string? Status { get; set; }
}

public class PayrollSuggestionViewDto
{
    public long Id { get; set; }
    public long BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public long AccountId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public string Title { get; set; } = string.Empty;
    public SalaryAdjustmentType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ApplyOption { get; set; } = "NextMonth";
    public bool IsResigned { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ManagerNote { get; set; }
    public long? ProcessedBy { get; set; }
    public string? ProcessorName { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public bool IsApplied { get; set; }
    public long? AppliedPayrollId { get; set; }
    public long CreatedBy { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
