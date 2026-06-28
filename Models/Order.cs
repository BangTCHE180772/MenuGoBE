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

    public string Method { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public long CustomerId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalAmount { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("TableId")]
    public Table Table { get; set; } = null!;

    [ForeignKey("FatherId")]
    public Order? Father { get; set; }

    [ForeignKey("CustomerId")]
    public Customer Customer { get; set; } = null!;

    [InverseProperty("Father")]
    public ICollection<Order> ChildOrders { get; set; } = new List<Order>();

    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
