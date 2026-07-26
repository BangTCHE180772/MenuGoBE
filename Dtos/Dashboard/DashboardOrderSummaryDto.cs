namespace MenuGoBE.Dtos.Dashboard
{
    public class DashboardOrderSummaryDto
    {
        public int New { get; set; }
        public int Paid { get; set; }
        public int Unpaid { get; set; }
        public int Cancelled { get; set; }
        public int Total { get; set; }
    }
}
