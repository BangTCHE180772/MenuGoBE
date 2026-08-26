namespace MenuGoBE.Dtos.Reservation;

public class ReservationCreateDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DateTime ReservationTime { get; set; }
    public int NumberOfGuests { get; set; }
    public string Note { get; set; } = string.Empty;
    public long? BranchId { get; set; }

    public List<long> TableIds { get; set; } = new List<long>();
    public int TableCount { get; set; }

    public List<PreOrderItemDto> PreOrderItems { get; set; } = new List<PreOrderItemDto>();

    public bool ConfirmUpdateCustomer { get; set; } = false;

    public bool IgnoreWarning { get; set; } = false;
}
