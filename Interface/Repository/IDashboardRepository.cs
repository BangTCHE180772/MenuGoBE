using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Dashboard;

namespace MenuGoBE.Interface.Repository
{
    public interface IDashboardRepository
    {
        Task<(decimal Revenue, decimal Expenses, decimal Profit, int CompletedBills, int TotalItemsSold, decimal AvgOrderValue)> GetFinancialStatsAsync(DateTime startDate, DateTime endDate, long? branchId);
        Task<(DashboardOrderSummaryDto OrderSummary, List<DashboardAlertDto> Alerts, List<DashboardCashFlowDto> CashFlowList)> GetTodayDetailStatsAsync(long? branchId);
        Task<List<DashboardChartPointDto>> GetDailyBreakdownAsync(DateTime startDate, DateTime endDate, long? branchId);
        Task<List<DashboardMonthlyPointDto>> GetMonthlyBreakdownAsync(int year, long? branchId);
    }
}
