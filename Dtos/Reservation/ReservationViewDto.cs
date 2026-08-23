namespace MenuGoBE.Dtos.Reservation;

public class ReservationViewDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DateTime ReservationTime { get; set; }
    public int NumberOfGuests { get; set; }
    public string Note { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long? OrderId { get; set; }
    public long? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    public List<string> TableNames { get; set; } = new List<string>();
    public int PreOrderItemsCount { get; set; }
    public List<PreOrderItemDto> PreOrderItems { get; set; } = new List<PreOrderItemDto>();
}
