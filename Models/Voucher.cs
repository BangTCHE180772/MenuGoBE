using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Voucher")]
public class Voucher
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    // Type: "Fixed", "Percentage"
    [Required]
    public string DiscountType { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,4)")]
    public decimal DiscountValue { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal MaxDiscount { get; set; } // Applied for Percentage

    [Column(TypeName = "decimal(18,4)")]
    public decimal MinOrderValue { get; set; }

    public long? BranchId { get; set; } // null means Global

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int Quantity { get; set; } // Total vouchers issued

    public int UsedCount { get; set; } = 0; // Vouchers redeemed

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Giới hạn số lần sử dụng mỗi khách hàng. null = không giới hạn.
    /// </summary>
    public int? MaxUsagePerCustomer { get; set; }

    // Navigation
    [ForeignKey("BranchId")]
    public Branch? Branch { get; set; }

    public ICollection<VoucherUsage> Usages { get; set; } = new List<VoucherUsage>();
}
