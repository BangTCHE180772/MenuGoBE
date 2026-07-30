namespace MenuGoBE.Dtos.Reservation;

public class ReservationUpdateDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DateTime ReservationTime { get; set; }
    public int NumberOfGuests { get; set; }
    public string Note { get; set; } = string.Empty;
}
