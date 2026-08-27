using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Reservations")]
public class Reservation
{
    [Key]
    public long Id { get; set; }

    public long CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public Customer? Customer { get; set; }

    public DateTime ReservationTime { get; set; }

    public int NumberOfGuests { get; set; }

    public string Note { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public long? BranchId { get; set; }

    [ForeignKey("BranchId")]
    public Branch? Branch { get; set; }

    public long? OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    public int TableCount { get; set; }

    public ICollection<ReservationDetail> ReservationDetails { get; set; } = new List<ReservationDetail>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
