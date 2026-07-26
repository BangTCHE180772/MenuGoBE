namespace MenuGoBE.Dtos.Dashboard
{
    public class DashboardChartPointDto
    {
        public string Date { get; set; } = string.Empty;
        public double Revenue { get; set; }
        public double Expense { get; set; }
        public double Profit { get; set; }
    }
}
