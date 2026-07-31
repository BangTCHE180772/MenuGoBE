using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Dashboard;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Service;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.BranchTest
{
    public class BranchServiceGetDashboardTests
    {
        private readonly Mock<IDashboardRepository> _repoMock;
        private readonly DashboardService _service;

        public BranchServiceGetDashboardTests()
        {
            _repoMock = new Mock<IDashboardRepository>();
            _service = new DashboardService(_repoMock.Object);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForDefaultRange()
        {
            // Arrange
            decimal expectedRevenue = 1000m;
            decimal expectedExpenses = 400m;
            decimal expectedProfit = 600m;
            int completedBills = 5;
            int totalItemsSold = 10;
            decimal avgOrderValue = 200m;
            var paymentMethods = new List<DashboardPaymentMethodDto>();
            var hourSlots = new List<DashboardHourSlotDto>();

            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
                .ReturnsAsync((expectedRevenue, expectedExpenses, expectedProfit, completedBills, totalItemsSold, avgOrderValue, paymentMethods, hourSlots, new List<DashboardDishDto>(), new List<DashboardDishDto>()));

            // Act
            var result = await _service.GetDashboardStatsAsync("thisMonth", null, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal((double)expectedRevenue, result.Revenue);
            Assert.Equal((double)expectedExpenses, result.Expenses);
            Assert.Equal((double)expectedProfit, result.Profit);

            _repoMock.Verify(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()), Times.Once);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_For7daysRange()
        {
            // Arrange
            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
                .ReturnsAsync((500m, 200m, 300m, 5, 10, 100m, new List<DashboardPaymentMethodDto>(), new List<DashboardHourSlotDto>(), new List<DashboardDishDto>(), new List<DashboardDishDto>()));

            var result = await _service.GetDashboardStatsAsync("7days", null, null, null);

            Assert.NotNull(result);
            Assert.Equal(500.0, result.Revenue);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForThisMonthRange()
        {
            // Arrange
            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
                .ReturnsAsync((800m, 300m, 500m, 8, 16, 100m, new List<DashboardPaymentMethodDto>(), new List<DashboardHourSlotDto>(), new List<DashboardDishDto>(), new List<DashboardDishDto>()));

            var result = await _service.GetDashboardStatsAsync("thisMonth", null, null, null);

            Assert.NotNull(result);
            Assert.Equal(800.0, result.Revenue);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForLastMonthRange()
        {
            // Arrange
            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
                .ReturnsAsync((1200m, 600m, 600m, 12, 24, 100m, new List<DashboardPaymentMethodDto>(), new List<DashboardHourSlotDto>(), new List<DashboardDishDto>(), new List<DashboardDishDto>()));

            var result = await _service.GetDashboardStatsAsync("lastMonth", null, null, null);

            Assert.NotNull(result);
            Assert.Equal(1200.0, result.Revenue);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForThisYearRange()
        {
            // Arrange
            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
                .ReturnsAsync((2000m, 900m, 1100m, 20, 40, 100m, new List<DashboardPaymentMethodDto>(), new List<DashboardHourSlotDto>(), new List<DashboardDishDto>(), new List<DashboardDishDto>()));

            var result = await _service.GetDashboardStatsAsync("thisYear", null, null, null);

            Assert.NotNull(result);
            Assert.Equal(2000.0, result.Revenue);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForLastYearRange()
        {
            // Arrange
            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
                .ReturnsAsync((3000m, 1500m, 1500m, 30, 60, 100m, new List<DashboardPaymentMethodDto>(), new List<DashboardHourSlotDto>(), new List<DashboardDishDto>(), new List<DashboardDishDto>()));

            var result = await _service.GetDashboardStatsAsync("lastYear", null, null, null);

            Assert.NotNull(result);
            Assert.Equal(3000.0, result.Revenue);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForCustomRange()
        {

            var startDateStr = "2026-07-01";
            var endDateStr = "2026-07-15";

            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
                .ReturnsAsync((150m, 50m, 100m, 3, 6, 50m, new List<DashboardPaymentMethodDto>(), new List<DashboardHourSlotDto>(), new List<DashboardDishDto>(), new List<DashboardDishDto>()));

            var result = await _service.GetDashboardStatsAsync("custom", startDateStr, endDateStr, null);

            Assert.NotNull(result);
            Assert.Equal(150.0, result.Revenue);

            _repoMock.Verify(r => r.GetFinancialStatsAsync(
                It.Is<DateTime>(dt => dt.Year == 2026 && dt.Month == 7 && dt.Day == 1),
                It.Is<DateTime>(dt => dt.Year == 2026 && dt.Month == 7 && dt.Day == 15),
                null), 
                Times.Once);
        }
    }
}
