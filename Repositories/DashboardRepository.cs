using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Dashboard;
using MenuGoBE.Interface.Repository;

namespace MenuGoBE.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;

        public DashboardRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(decimal Revenue, decimal Expenses, decimal Profit, int CompletedBills, int TotalItemsSold, decimal AvgOrderValue, List<DashboardPaymentMethodDto> PaymentMethods, List<DashboardHourSlotDto> HourSlots, List<DashboardDishDto> TopDishes, List<DashboardDishDto> LeastDishes)> GetFinancialStatsAsync(DateTime startDate, DateTime endDate, long? branchId)
        {
            var paymentsQuery = _context.Payments.AsNoTracking()
                .Where(p => p.Status == "Success" && p.CreatedAt >= startDate && p.CreatedAt <= endDate);

            if (branchId.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.Order.Table.Area.BranchId == branchId.Value);
            }

            var paidPayments = await paymentsQuery.ToListAsync();
            var revenue = paidPayments.Sum(p => p.Amount);
            var completedBills = paidPayments.Count;

            var paidOrderIds = paidPayments.Select(p => p.OrderId).Distinct().ToList();
            var totalItemsSold = await _context.OrderDetails
                .Where(od => paidOrderIds.Contains(od.OrderId))
                .SumAsync(od => od.Quantity);

            var expensesQuery = _context.CashFlows.AsNoTracking()
                .Where(cf => !cf.IsDeleted && (int)cf.Direction == 2 && cf.BusinessDate >= startDate && cf.BusinessDate <= endDate);

            if (branchId.HasValue)
            {
                expensesQuery = expensesQuery.Where(cf => cf.BranchId == branchId.Value);
            }

            var expenses = await expensesQuery.SumAsync(cf => cf.TotalAmount);

            var profit = revenue - expenses;
            var avgOrderValue = completedBills > 0 ? revenue / completedBills : 0m;

            // Group Payment Methods
            var paymentMethods = paidPayments
                .GroupBy(p => p.Method)
                .Select(g => new DashboardPaymentMethodDto
                {
                    Name = g.Key == "Cash" ? "Tiền mặt" : (g.Key == "PayOS" ? "Chuyển khoản / QR" : g.Key),
                    Value = (double)g.Sum(p => p.Amount)
                })
                .ToList();

            // Group Hour Slots
            var morningSum = 0m;
            var noonSum = 0m;
            var afternoonSum = 0m;
            var eveningSum = 0m;

            foreach (var p in paidPayments)
            {
                var localHour = p.CreatedAt.AddHours(7).Hour;
                if (localHour >= 6 && localHour < 11)
                    morningSum += p.Amount;
                else if (localHour >= 11 && localHour < 14)
                    noonSum += p.Amount;
                else if (localHour >= 14 && localHour < 18)
                    afternoonSum += p.Amount;
                else
                    eveningSum += p.Amount;
            }

            var hourSlots = new List<DashboardHourSlotDto>
            {
                new() { Slot = "Sáng (6-11h)", Value = (double)morningSum },
                new() { Slot = "Trưa (11-14h)", Value = (double)noonSum },
                new() { Slot = "Chiều (14-18h)", Value = (double)afternoonSum },
                new() { Slot = "Tối (18-22h)", Value = (double)eveningSum }
            };

            // Get ChainId if branchId is provided
            long? chainId = null;
            if (branchId.HasValue)
            {
                var branch = await _context.Branches.FindAsync(branchId.Value);
                if (branch != null)
                {
                    chainId = branch.ChainId;
                }
            }

            var productsQuery = _context.Products.AsNoTracking().AsQueryable();
            List<string> allProducts;
            if (chainId.HasValue && chainId.Value > 0)
            {
                var chainProducts = await productsQuery.Where(p => p.ChainId == chainId.Value).Select(p => p.Name).Distinct().ToListAsync();
                allProducts = chainProducts.Any() ? chainProducts : await productsQuery.Select(p => p.Name).Distinct().ToListAsync();
            }
            else
            {
                allProducts = await productsQuery.Select(p => p.Name).Distinct().ToListAsync();
            }

            // Group Top/Least Dishes from paid orders
            var realSales = await _context.OrderDetails
                .Where(od => paidOrderIds.Contains(od.OrderId))
                .GroupBy(od => od.Product.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    Quantity = g.Sum(od => od.Quantity)
                })
                .ToDictionaryAsync(x => x.Name, x => x.Quantity);

            // Union with product names from real sales to guarantee no sold dish is lost
            var salesProductNames = realSales.Keys.ToList();
            var fullProductList = allProducts.Union(salesProductNames).Distinct().ToList();

            var allDishesWithQty = fullProductList.Select(name => new DashboardDishDto
            {
                Name = name,
                Quantity = realSales.TryGetValue(name, out var qty) ? qty : 0
            }).ToList();

            var topDishes = allDishesWithQty.OrderByDescending(d => d.Quantity).Take(10).ToList();
            var leastDishes = allDishesWithQty.OrderBy(d => d.Quantity).Take(10).ToList();

            return (revenue, expenses, profit, completedBills, totalItemsSold, avgOrderValue, paymentMethods, hourSlots, topDishes, leastDishes);
        }

        public async Task<(DashboardOrderSummaryDto OrderSummary, List<DashboardAlertDto> Alerts, List<DashboardCashFlowDto> CashFlowList)> GetTodayDetailStatsAsync(long? branchId)
        {
            var todayLocal = DateTime.UtcNow.AddHours(7).Date;
            var startUtc = DateTime.SpecifyKind(todayLocal.AddHours(-7), DateTimeKind.Utc);
            var endUtc = DateTime.SpecifyKind(todayLocal.AddDays(1).AddHours(-7).AddTicks(-1), DateTimeKind.Utc);

            var todayOrdersQuery = _context.Orders.AsNoTracking().Where(o => o.CreatedAt >= startUtc && o.CreatedAt <= endUtc);
            if (branchId.HasValue)
            {
                todayOrdersQuery = todayOrdersQuery.Where(o => o.Table.Area.BranchId == branchId.Value);
            }

            var ordersToday = await todayOrdersQuery.ToListAsync();
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);

            var paidCount = ordersToday.Count(o => o.Status == "Paid");
            var cancelledCount = ordersToday.Count(o => o.Status == "Cancelled");
            var unpaidCount = ordersToday.Count(o => o.Status == "Active" && o.CreatedAt < oneHourAgo);
            var newCount = ordersToday.Count(o => o.Status == "Active" && o.CreatedAt >= oneHourAgo);

            var orderSummary = new DashboardOrderSummaryDto
            {
                New = newCount,
                Paid = paidCount,
                Unpaid = unpaidCount,
                Cancelled = cancelledCount,
                Total = paidCount + cancelledCount + unpaidCount + newCount
            };

            var alerts = new List<DashboardAlertDto>();

            if (unpaidCount > 0)
            {
                alerts.Add(new DashboardAlertDto
                {
                    Level = "warning",
                    Message = $"{unpaidCount} đơn hàng chưa thanh toán"
                });
            }

            var lowStockQuery = _context.BInventories.Where(bi => bi.IsManageQuantity && bi.Quantity <= bi.MinStorage);
            if (branchId.HasValue)
            {
                lowStockQuery = lowStockQuery.Where(bi => bi.BranchId == branchId.Value);
            }
            var lowStockCount = await lowStockQuery.CountAsync();
            if (lowStockCount > 0)
            {
                alerts.Add(new DashboardAlertDto
                {
                    Level = "warning",
                    Message = $"{lowStockCount} nguyên liệu/sản phẩm sắp hết tồn kho"
                });
            }

            var todayDateOnly = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var noCheckInQuery = _context.WorkSchedules.Where(ws => ws.WorkDate == todayDateOnly && ws.CheckInAt == null);
            if (branchId.HasValue)
            {
                noCheckInQuery = noCheckInQuery.Where(ws => ws.BranchId == branchId.Value);
            }
            var noCheckInCount = await noCheckInQuery.CountAsync();
            if (noCheckInCount > 0)
            {
                alerts.Add(new DashboardAlertDto
                {
                    Level = "warning",
                    Message = $"{noCheckInCount} nhân viên chưa chấm công hôm nay"
                });
            }

            var todayExpensesQuery = _context.CashFlows
                .Where(cf => !cf.IsDeleted && (int)cf.Direction == 2 && cf.BusinessDate >= startUtc && cf.BusinessDate <= endUtc);
            if (branchId.HasValue)
            {
                todayExpensesQuery = todayExpensesQuery.Where(cf => cf.BranchId == branchId.Value);
            }
            var todayExpenses = await todayExpensesQuery.SumAsync(cf => cf.TotalAmount);
            if (todayExpenses > 50000000)
            {
                alerts.Add(new DashboardAlertDto
                {
                    Level = "danger",
                    Message = $"Chi phí hôm nay tăng cao đột biến: {todayExpenses:N0}đ"
                });
            }

            // 1. Query Payments today
            var paymentsQuery = _context.Payments.AsNoTracking()
                .Where(p => p.Status == "Success" && p.CreatedAt >= startUtc && p.CreatedAt <= endUtc);
            if (branchId.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.Order.Table.Area.BranchId == branchId.Value);
            }
            var paymentsToday = await paymentsQuery.ToListAsync();

            // 2. Query CashFlows today
            var cashFlowsTodayQuery = _context.CashFlows.AsNoTracking()
                .Where(cf => !cf.IsDeleted && cf.BusinessDate >= startUtc && cf.BusinessDate <= endUtc);
            if (branchId.HasValue)
            {
                cashFlowsTodayQuery = cashFlowsTodayQuery.Where(cf => cf.BranchId == branchId.Value);
            }
            var cashFlowsToday = await cashFlowsTodayQuery.ToListAsync();

            // 3. Merge both into DashboardCashFlowDto list
            var cashFlowList = new List<DashboardCashFlowDto>();

            foreach (var p in paymentsToday)
            {
                cashFlowList.Add(new DashboardCashFlowDto
                {
                    Id = p.Id,
                    Time = p.CreatedAt.AddHours(7).ToString("HH:mm"),
                    Type = "in",
                    Category = "Thanh toán hóa đơn",
                    Amount = (double)p.Amount
                });
            }

            foreach (var cf in cashFlowsToday)
            {
                var directionStr = (int)cf.Direction == 1 ? "in" : "out";
                var categoryStr = "Thu khác";
                if ((int)cf.Direction == 2)
                {
                    if (cf.Code.StartsWith("PAYROLL", StringComparison.OrdinalIgnoreCase))
                        categoryStr = "Trả lương";
                    else if (cf.Code.StartsWith("IMPORT", StringComparison.OrdinalIgnoreCase))
                        categoryStr = "Nhập hàng";
                    else if (cf.Code.StartsWith("UTILITY", StringComparison.OrdinalIgnoreCase))
                        categoryStr = "Điện nước";
                    else
                        categoryStr = "Chi phí khác";
                }

                cashFlowList.Add(new DashboardCashFlowDto
                {
                    Id = cf.Id + 1000000, // ensure uniqueness of ID
                    Time = cf.CreatedAt.AddHours(7).ToString("HH:mm"),
                    Type = directionStr,
                    Category = categoryStr,
                    Amount = (int)cf.Direction == 2 ? -(double)cf.TotalAmount : (double)cf.TotalAmount
                });
            }

            var sortedCashFlowList = cashFlowList.OrderByDescending(cf => cf.Time).ToList();

            return (orderSummary, alerts, sortedCashFlowList);
        }

        public async Task<List<DashboardChartPointDto>> GetDailyBreakdownAsync(DateTime startDate, DateTime endDate, long? branchId)
        {
            var paymentsQuery = _context.Payments.AsNoTracking()
                .Where(p => p.Status == "Success" && p.CreatedAt >= startDate && p.CreatedAt <= endDate);

            if (branchId.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.Order.Table.Area.BranchId == branchId.Value);
            }

            var payments = await paymentsQuery.ToListAsync();

            // Group by Date in UTC
            var paymentsGrouped = payments
                .GroupBy(p => p.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(p => p.Amount) })
                .ToList();

            var expensesQuery = _context.CashFlows.AsNoTracking()
                .Where(cf => !cf.IsDeleted && (int)cf.Direction == 2 && cf.BusinessDate >= startDate && cf.BusinessDate <= endDate);

            if (branchId.HasValue)
            {
                expensesQuery = expensesQuery.Where(cf => cf.BranchId == branchId.Value);
            }

            var expenses = await expensesQuery.ToListAsync();

            // Group by BusinessDate Date
            var expensesGrouped = expenses
                .GroupBy(cf => cf.BusinessDate.Date)
                .Select(g => new { Date = g.Key, Expenses = g.Sum(cf => cf.TotalAmount) })
                .ToList();

            var result = new List<DashboardChartPointDto>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var dayRevenue = paymentsGrouped.FirstOrDefault(g => g.Date == date)?.Revenue ?? 0m;
                var dayExpenses = expensesGrouped.FirstOrDefault(g => g.Date == date)?.Expenses ?? 0m;


                result.Add(new DashboardChartPointDto
                {
                    Date = date.ToString("dd/MM"),
                    Revenue = (double)dayRevenue,
                    Expense = (double)dayExpenses,
                    Profit = (double)(dayRevenue - dayExpenses)
                });
            }

            return result;
        }

        public async Task<List<DashboardMonthlyPointDto>> GetMonthlyBreakdownAsync(int year, long? branchId)
        {
            var yearStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var yearEnd = new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

            var paymentsQuery = _context.Payments.AsNoTracking()
                .Where(p => p.Status == "Success" && p.CreatedAt >= yearStart && p.CreatedAt <= yearEnd);

            if (branchId.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.Order.Table.Area.BranchId == branchId.Value);
            }

            var payments = await paymentsQuery.ToListAsync();

            var paymentsGrouped = payments
                .GroupBy(p => p.CreatedAt.Month)
                .Select(g => new { Month = g.Key, Revenue = g.Sum(p => p.Amount) })
                .ToList();

            var expensesQuery = _context.CashFlows.AsNoTracking()
                .Where(cf => !cf.IsDeleted && (int)cf.Direction == 2 && cf.BusinessDate >= yearStart && cf.BusinessDate <= yearEnd);

            if (branchId.HasValue)
            {
                expensesQuery = expensesQuery.Where(cf => cf.BranchId == branchId.Value);
            }

            var expenses = await expensesQuery.ToListAsync();

            var expensesGrouped = expenses
                .GroupBy(cf => cf.BusinessDate.Month)
                .Select(g => new { Month = g.Key, Expenses = g.Sum(cf => cf.TotalAmount) })
                .ToList();

            var result = new List<DashboardMonthlyPointDto>();
            for (int m = 1; m <= 12; m++)
            {
                var mRevenue = paymentsGrouped.FirstOrDefault(g => g.Month == m)?.Revenue ?? 0m;
                var mExpenses = expensesGrouped.FirstOrDefault(g => g.Month == m)?.Expenses ?? 0m;



                result.Add(new DashboardMonthlyPointDto
                {
                    Month = $"T{m}",
                    Revenue = (double)mRevenue,
                    Expense = (double)mExpenses,
                    Profit = (double)(mRevenue - mExpenses)
                });
            }

            return result;
        }

        public async Task<List<DashboardCashFlowDto>> GetCashFlowListAsync(DateTime startDate, DateTime endDate, long? branchId)
        {
            var paymentsQuery = _context.Payments.AsNoTracking()
                .Where(p => p.Status == "Success" && p.CreatedAt >= startDate && p.CreatedAt <= endDate);

            if (branchId.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.Order.Table.Area.BranchId == branchId.Value);
            }

            var payments = await paymentsQuery.ToListAsync();

            var expensesQuery = _context.CashFlows.AsNoTracking()
                .Where(cf => !cf.IsDeleted && cf.BusinessDate >= startDate && cf.BusinessDate <= endDate);

            if (branchId.HasValue)
            {
                expensesQuery = expensesQuery.Where(cf => cf.BranchId == branchId.Value);
            }

            var expenses = await expensesQuery.ToListAsync();

            var cashFlowList = new List<DashboardCashFlowDto>();

            foreach (var p in payments)
            {
                cashFlowList.Add(new DashboardCashFlowDto
                {
                    Id = p.Id,
                    Time = p.CreatedAt.AddHours(7).ToString("yyyy-MM-dd HH:mm"),
                    Type = "in",
                    Category = "Thanh toán hóa đơn",
                    Amount = (double)p.Amount
                });
            }

            foreach (var cf in expenses)
            {
                var directionStr = (int)cf.Direction == 1 ? "in" : "out";
                var categoryStr = "Thu khác";
                if ((int)cf.Direction == 2)
                {
                    if (cf.Code.StartsWith("PAYROLL", StringComparison.OrdinalIgnoreCase))
                        categoryStr = "Trả lương";
                    else if (cf.Code.StartsWith("IMPORT", StringComparison.OrdinalIgnoreCase))
                        categoryStr = "Nhập hàng";
                    else if (cf.Code.StartsWith("UTILITY", StringComparison.OrdinalIgnoreCase))
                        categoryStr = "Điện nước";
                    else
                        categoryStr = "Chi phí khác";
                }

                cashFlowList.Add(new DashboardCashFlowDto
                {
                    Id = cf.Id + 1000000,
                    Time = cf.CreatedAt.AddHours(7).ToString("yyyy-MM-dd HH:mm"),
                    Type = directionStr,
                    Category = categoryStr,
                    Amount = (int)cf.Direction == 2 ? -(double)cf.TotalAmount : (double)cf.TotalAmount
                });
            }

            return cashFlowList.OrderByDescending(cf => cf.Time).ToList();
        }
    }
}
