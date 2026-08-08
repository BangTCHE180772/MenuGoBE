namespace MenuGoBE.Dtos.Document
{
    public class InvoiceOutflowLineDto
    {
        public long ProductId { get; set; }
        public decimal Quantity { get; set; }
        public string? Note { get; set; }
    }
}
