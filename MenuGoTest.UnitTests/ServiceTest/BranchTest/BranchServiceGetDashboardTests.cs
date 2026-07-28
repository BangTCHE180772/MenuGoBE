using System;
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

            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
                .ReturnsAsync((expectedRevenue, expectedExpenses, expectedProfit, 10, 50, 100m));

            // Act
            var result = await _service.GetDashboardStatsAsync("default", null, null, null);

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
            decimal expectedRevenue = 1000m;
            decimal expectedExpenses = 400m;
            decimal expectedProfit = 600m;

            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
                .ReturnsAsync((expectedRevenue, expectedExpenses, expectedProfit, 10, 50, 100m));

            var result = await _service.GetDashboardStatsAsync("7days", null, null, null);

            Assert.NotNull(result);
            Assert.Equal((double)expectedRevenue, result.Revenue);
            Assert.Equal((double)expectedExpenses, result.Expenses);
            Assert.Equal((double)expectedProfit, result.Profit);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForThisMonthRange()
        {
            decimal expectedRevenue = 1000m;
            decimal expectedExpenses = 400m;
            decimal expectedProfit = 600m;

            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
                .ReturnsAsync((expectedRevenue, expectedExpenses, expectedProfit, 10, 50, 100m));

            var result = await _service.GetDashboardStatsAsync("thisMonth", null, null, null);

            Assert.NotNull(result);
            Assert.Equal((double)expectedRevenue, result.Revenue);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForLastMonthRange()
        {
            decimal expectedRevenue = 1000m;
            decimal expectedExpenses = 400m;
            decimal expectedProfit = 600m;

            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
                .ReturnsAsync((expectedRevenue, expectedExpenses, expectedProfit, 10, 50, 100m));

            var result = await _service.GetDashboardStatsAsync("lastMonth", null, null, null);

            Assert.NotNull(result);
            Assert.Equal((double)expectedRevenue, result.Revenue);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForThisYearRange()
        {
            decimal expectedRevenue = 1000m;
            decimal expectedExpenses = 400m;
            decimal expectedProfit = 600m;

            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
                .ReturnsAsync((expectedRevenue, expectedExpenses, expectedProfit, 10, 50, 100m));

            var result = await _service.GetDashboardStatsAsync("thisYear", null, null, null);

            Assert.NotNull(result);
            Assert.Equal((double)expectedRevenue, result.Revenue);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForLastYearRange()
        {
            decimal expectedRevenue = 1000m;
            decimal expectedExpenses = 400m;
            decimal expectedProfit = 600m;

            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
                .ReturnsAsync((expectedRevenue, expectedExpenses, expectedProfit, 10, 50, 100m));

            var result = await _service.GetDashboardStatsAsync("lastYear", null, null, null);

            Assert.NotNull(result);
            Assert.Equal((double)expectedRevenue, result.Revenue);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForCustomRange()
        {
            decimal expectedRevenue = 1000m;
            decimal expectedExpenses = 400m;
            decimal expectedProfit = 600m;

            var startDateStr = DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-dd");
            var endDateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");

            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
                .ReturnsAsync((expectedRevenue, expectedExpenses, expectedProfit, 10, 50, 100m));

            var result = await _service.GetDashboardStatsAsync("custom", startDateStr, endDateStr, null);

            Assert.NotNull(result);
            Assert.Equal((double)expectedRevenue, result.Revenue);
        }
    }
}
