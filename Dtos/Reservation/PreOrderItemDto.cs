namespace MenuGoBE.Dtos.Reservation;

public class PreOrderItemDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string Note { get; set; } = string.Empty;
}