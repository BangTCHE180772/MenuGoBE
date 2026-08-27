using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("ReservationDetails")]
public class ReservationDetail
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long ReservationId { get; set; }

    [ForeignKey("ReservationId")]
    public Reservation? Reservation { get; set; }

    [Required]
    public long ProductId { get; set; }

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    public string Note { get; set; } = string.Empty;
}
