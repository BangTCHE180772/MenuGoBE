using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Customer")]
public class Customer
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public int Point { get; set; }

    public string? HashedPassword { get; set; }

    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Khách hàng có muốn nhận email thông báo khuyến mãi không
    /// </summary>
    public bool ReceivePromoEmails { get; set; } = true;

    // Navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
