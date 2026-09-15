namespace MenuGoBE.Dtos.Reservation;

public class ReservationEmailNotification
{
    public long CustomerId { get; set; }
    public string? CustomerEmail { get; set; }
    public string CustomerName { get; set; } = "";

    /// <summary>
    /// "Created", "Confirmed", "CheckedIn", "Cancelled", "Rejected",
    /// "Reminder30Min", "LateWarning15Min", "AutoCancelled30Min"
    /// </summary>
    public string EventType { get; set; } = "";

    public DateTime ReservationTime { get; set; }
    public string BranchName { get; set; } = "";
    public long ReservationId { get; set; }
    public string? TableNames { get; set; }
}
