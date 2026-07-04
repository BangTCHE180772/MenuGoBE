namespace MenuGoBE.Dtos.Order
{
    public class OrderMergeDto
    {
        public long ChildOrderId { get; set; }

        public long FatherOrderId { get; set; }
    }
}
