using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Voucher;

public class VoucherUpdateDto
{
    [Required]
    public long Id { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string DiscountType { get; set; } = string.Empty;

    public decimal DiscountValue { get; set; }
    public decimal MaxDiscount { get; set; }
    public decimal MinOrderValue { get; set; }

    public long? BranchId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int Quantity { get; set; }
    public bool IsActive { get; set; }
    public int? MaxUsagePerCustomer { get; set; }
}
