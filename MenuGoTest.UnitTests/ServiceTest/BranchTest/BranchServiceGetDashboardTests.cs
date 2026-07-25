using System;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Service;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.BranchTest
{
    public class BranchServiceGetDashboardTests
    {
        private readonly Mock<IBranchRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly BranchService _service;

        public BranchServiceGetDashboardTests()
        {
            _repoMock = new Mock<IBranchRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new BranchService(_repoMock.Object, _mapper);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForDefaultRange()
        {
            // Arrange
            decimal expectedRevenue = 1000m;
            decimal expectedExpenses = 400m;
            decimal expectedProfit = 600m;

            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync((expectedRevenue, expectedExpenses, expectedProfit));

            // Act
            var result = await _service.GetDashboardStatsAsync("default", null, null);

            // Assert
            Assert.NotNull(result);
            var revenueProp = result.GetType().GetProperty("revenue");
            var expensesProp = result.GetType().GetProperty("expenses");
            var profitProp = result.GetType().GetProperty("profit");

            Assert.NotNull(revenueProp);
            Assert.NotNull(expensesProp);
            Assert.NotNull(profitProp);

            Assert.Equal((double)expectedRevenue, (double)revenueProp.GetValue(result)!);
            Assert.Equal((double)expectedExpenses, (double)expensesProp.GetValue(result)!);
            Assert.Equal((double)expectedProfit, (double)profitProp.GetValue(result)!);

            _repoMock.Verify(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_For7daysRange()
        {
            // Arrange
            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync((500m, 200m, 300m));

            // Act
            var result = await _service.GetDashboardStatsAsync("7days", null, null);

            // Assert
            Assert.NotNull(result);
            var revenueProp = result.GetType().GetProperty("revenue");
            Assert.Equal(500.0, (double)revenueProp!.GetValue(result)!);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForThisMonthRange()
        {
            // Arrange
            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync((800m, 300m, 500m));

            // Act
            var result = await _service.GetDashboardStatsAsync("thisMonth", null, null);

            // Assert
            Assert.NotNull(result);
            var revenueProp = result.GetType().GetProperty("revenue");
            Assert.Equal(800.0, (double)revenueProp!.GetValue(result)!);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForLastMonthRange()
        {
            // Arrange
            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync((1200m, 600m, 600m));

            // Act
            var result = await _service.GetDashboardStatsAsync("lastMonth", null, null);

            // Assert
            Assert.NotNull(result);
            var revenueProp = result.GetType().GetProperty("revenue");
            Assert.Equal(1200.0, (double)revenueProp!.GetValue(result)!);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForThisYearRange()
        {
            // Arrange
            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync((2000m, 900m, 1100m));

            // Act
            var result = await _service.GetDashboardStatsAsync("thisYear", null, null);

            // Assert
            Assert.NotNull(result);
            var revenueProp = result.GetType().GetProperty("revenue");
            Assert.Equal(2000.0, (double)revenueProp!.GetValue(result)!);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForLastYearRange()
        {
            // Arrange
            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync((3000m, 1500m, 1500m));

            // Act
            var result = await _service.GetDashboardStatsAsync("lastYear", null, null);

            // Assert
            Assert.NotNull(result);
            var revenueProp = result.GetType().GetProperty("revenue");
            Assert.Equal(3000.0, (double)revenueProp!.GetValue(result)!);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnFinancialStats_ForCustomRange()
        {
            // Arrange
            _repoMock.Setup(r => r.GetFinancialStatsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync((150m, 50m, 100m));

            string startDateStr = "2026-07-01";
            string endDateStr = "2026-07-15";

            // Act
            var result = await _service.GetDashboardStatsAsync("custom", startDateStr, endDateStr);

            // Assert
            Assert.NotNull(result);
            var revenueProp = result.GetType().GetProperty("revenue");
            Assert.Equal(150.0, (double)revenueProp!.GetValue(result)!);

            _repoMock.Verify(r => r.GetFinancialStatsAsync(
                It.Is<DateTime>(dt => dt.Year == 2026 && dt.Month == 7 && dt.Day == 1),
                It.Is<DateTime>(dt => dt.Year == 2026 && dt.Month == 7 && dt.Day == 15)), 
                Times.Once);
        }
    }
}
