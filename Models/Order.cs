using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Orders")]
public class Order
{
    [Key]
    public long Id { get; set; }

    public long TableId { get; set; }

    public long? FatherId { get; set; }

    public string Status { get; set; } = string.Empty;

    public long? CustomerId { get; set; }

    public long? VoucherId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DiscountAmount { get; set; }

    public int PointsUsed { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalAmount { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("TableId")]
    public Table Table { get; set; } = null!;

    [ForeignKey("FatherId")]
    public Order? Father { get; set; }

    [ForeignKey("CustomerId")]
    public Customer? Customer { get; set; }

    [ForeignKey("VoucherId")]
    public Voucher? Voucher { get; set; }

    [InverseProperty("Father")]
    public ICollection<Order> ChildOrders { get; set; } = new List<Order>();

    [InverseProperty("Order")]
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public ICollection<OrderAssignment> Assignments { get; set; } = new List<OrderAssignment>();
}
