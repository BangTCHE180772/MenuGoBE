namespace MenuGoBE.Dtos.Dashboard
{
    public class DashboardCashFlowDto
    {
        public long Id { get; set; }
        public string Time { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "in" or "out"
        public string Category { get; set; } = string.Empty;
        public double Amount { get; set; }
    }
}
