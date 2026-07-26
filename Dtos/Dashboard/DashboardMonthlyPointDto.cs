namespace MenuGoBE.Dtos.Dashboard
{
    public class DashboardMonthlyPointDto
    {
        public string Month { get; set; } = string.Empty;
        public double Revenue { get; set; }
        public double Expense { get; set; }
        public double Profit { get; set; }
    }
}
