using System.Collections.Generic;

namespace MenuGoBE.Dtos.Dashboard
{
    public class DashboardStatsDto
    {
        public double Revenue { get; set; }
        public double Expenses { get; set; }
        public double Profit { get; set; }
        public int CompletedBills { get; set; }
        public int TotalItemsSold { get; set; }
        public double AvgOrderValue { get; set; }
        public DashboardOrderSummaryDto? OrderSummary { get; set; }
        public List<DashboardAlertDto>? Alerts { get; set; }
        public List<DashboardCashFlowDto>? CashFlowList { get; set; }
        public List<DashboardChartPointDto>? DailyStats { get; set; }
        public List<DashboardMonthlyPointDto>? MonthlyStats { get; set; }
        public List<DashboardPaymentMethodDto> PaymentMethods { get; set; } = new();
        public List<DashboardHourSlotDto> HourSlots { get; set; } = new();
        public List<DashboardDishDto> TopDishes { get; set; } = new();
        public List<DashboardDishDto> LeastDishes { get; set; } = new();
    }
}
