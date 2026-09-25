using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Dashboard;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;

namespace MenuGoBE.Service
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _repo;

        public DashboardService(IDashboardRepository repo)
        {
            _repo = repo;
        }

        #region Lấy dữ liệu thống kê tổng quan Dashboard theo mốc thời gian và chi nhánh
        public async Task<DashboardStatsDto> GetDashboardStatsAsync(string range, string? startDateStr, string? endDateStr, long? branchId)
        {
            var todayLocal = DateTime.UtcNow.AddHours(7).Date;
            DateTime startDate;
            DateTime endDate;

            // Xác định khoảng thời gian truy vấn theo múi giờ Việt Nam (UTC+7)
            if (range == "today")
            {
                startDate = DateTime.SpecifyKind(todayLocal.AddHours(-7), DateTimeKind.Utc);
                endDate = DateTime.SpecifyKind(todayLocal.AddDays(1).AddHours(-7).AddTicks(-1), DateTimeKind.Utc);
            }
            else if (range == "yesterday")
            {
                var yesterdayLocal = todayLocal.AddDays(-1);
                startDate = DateTime.SpecifyKind(yesterdayLocal.AddHours(-7), DateTimeKind.Utc);
                endDate = DateTime.SpecifyKind(yesterdayLocal.AddDays(1).AddHours(-7).AddTicks(-1), DateTimeKind.Utc);
            }
            else if (range == "7days")
            {
                startDate = DateTime.SpecifyKind(todayLocal.AddDays(-6).AddHours(-7), DateTimeKind.Utc);
                endDate = DateTime.SpecifyKind(todayLocal.AddDays(1).AddHours(-7).AddTicks(-1), DateTimeKind.Utc);
            }
            else if (range == "thisMonth")
            {
                var firstDayThisMonth = new DateTime(todayLocal.Year, todayLocal.Month, 1);
                startDate = DateTime.SpecifyKind(firstDayThisMonth.AddHours(-7), DateTimeKind.Utc);
                endDate = DateTime.SpecifyKind(todayLocal.AddDays(1).AddHours(-7).AddTicks(-1), DateTimeKind.Utc);
            }
            else if (range == "lastMonth")
            {
                var firstDayThisMonth = new DateTime(todayLocal.Year, todayLocal.Month, 1);
                var firstDayLastMonth = firstDayThisMonth.AddMonths(-1);
                startDate = DateTime.SpecifyKind(firstDayLastMonth.AddHours(-7), DateTimeKind.Utc);
                endDate = DateTime.SpecifyKind(firstDayThisMonth.AddHours(-7).AddTicks(-1), DateTimeKind.Utc);
            }
            else if (range == "thisYear")
            {
                startDate = DateTime.SpecifyKind(new DateTime(todayLocal.Year, 1, 1).AddHours(-7), DateTimeKind.Utc);
                endDate = DateTime.SpecifyKind(todayLocal.AddDays(1).AddHours(-7).AddTicks(-1), DateTimeKind.Utc);
            }
            else if (range == "lastYear")
            {
                startDate = DateTime.SpecifyKind(new DateTime(todayLocal.Year - 1, 1, 1).AddHours(-7), DateTimeKind.Utc);
                endDate = DateTime.SpecifyKind(new DateTime(todayLocal.Year, 1, 1).AddHours(-7).AddTicks(-1), DateTimeKind.Utc);
            }
            else if (range == "custom" && !string.IsNullOrEmpty(startDateStr) && !string.IsNullOrEmpty(endDateStr))
            {
                startDate = DateTime.SpecifyKind(new DateTime(todayLocal.Year, todayLocal.Month, 1).AddHours(-7), DateTimeKind.Utc);
                endDate = DateTime.SpecifyKind(todayLocal.AddDays(1).AddHours(-7).AddTicks(-1), DateTimeKind.Utc);
                if (DateTime.TryParse(startDateStr, out var parsedStart))
                {
                    startDate = DateTime.SpecifyKind(parsedStart.Date.AddHours(-7), DateTimeKind.Utc);
                }
                if (DateTime.TryParse(endDateStr, out var parsedEnd))
                {
                    endDate = DateTime.SpecifyKind(parsedEnd.Date.AddDays(1).AddHours(-7).AddTicks(-1), DateTimeKind.Utc);
                }
            }
            else
            {
                var firstDayThisMonth = new DateTime(todayLocal.Year, todayLocal.Month, 1);
                startDate = DateTime.SpecifyKind(firstDayThisMonth.AddHours(-7), DateTimeKind.Utc);
                endDate = DateTime.SpecifyKind(todayLocal.AddDays(1).AddHours(-7).AddTicks(-1), DateTimeKind.Utc);
            }

            var (revenue, expenses, profit, completedBills, totalItemsSold, avgOrderValue, paymentMethods, hourSlots, topDishes, leastDishes) =
                await _repo.GetFinancialStatsAsync(startDate, endDate, branchId);

            DashboardOrderSummaryDto? orderSummary = null;
            List<DashboardAlertDto>? alerts = null;

            if (range == "today")
            {
                var detailStats = await _repo.GetTodayDetailStatsAsync(branchId);
                orderSummary = detailStats.OrderSummary;
                alerts = detailStats.Alerts;
            }

            var cashFlowList = await _repo.GetCashFlowListAsync(startDate, endDate, branchId);
            var dailyStats = await _repo.GetDailyBreakdownAsync(startDate, endDate, branchId);
            var monthlyStats = await _repo.GetMonthlyBreakdownAsync(todayLocal.Year, branchId);

            return new DashboardStatsDto
            {
                Revenue = (double)revenue,
                Expenses = (double)expenses,
                Profit = (double)profit,
                CompletedBills = completedBills,
                TotalItemsSold = totalItemsSold,
                AvgOrderValue = (double)avgOrderValue,
                OrderSummary = orderSummary,
                Alerts = alerts,
                CashFlowList = cashFlowList,
                DailyStats = dailyStats,
                MonthlyStats = monthlyStats,
                PaymentMethods = paymentMethods,
                HourSlots = hourSlots,
                TopDishes = topDishes,
                LeastDishes = leastDishes
            };
        }
        #endregion
    }
}
