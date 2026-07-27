using MenuGoBE.Dtos.Voucher;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.VoucherServiceTest;

public class GetByIdAsyncTests
{
    private readonly Mock<IVoucherRepository> _repoMock;
    private readonly VoucherService _service;

    public GetByIdAsyncTests()
    {
        _repoMock = new Mock<IVoucherRepository>();
        _service = new VoucherService(_repoMock.Object);
    }

    [Fact]
    public async Task ReturnsDto_WhenExists()
    {
        // Arrange
        var voucher = new Voucher { Id = 1, Code = "SALE10", Name = "Giảm 10%", DiscountType = "Percentage", DiscountValue = 10, MaxDiscount = 50000, MinOrderValue = 100000, Quantity = 100, UsedCount = 5, IsActive = true, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30) };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(voucher);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("SALE10", result.Code);
    }

    [Fact]
    public async Task ReturnsNull_WhenNotExists()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Voucher?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task MapsAllFields_Correctly()
    {
        // Arrange
        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow.AddDays(7);
        var voucher = new Voucher
        {
            Id = 5, Code = "TEST", Name = "Test V", DiscountType = "Fixed",
            DiscountValue = 30000, MaxDiscount = 0, MinOrderValue = 100000,
            BranchId = 3, Quantity = 20, UsedCount = 8, IsActive = false,
            StartDate = start, EndDate = end,
            Branch = new Branch { Name = "CN3" }
        };
        _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(voucher);

        // Act
        var result = await _service.GetByIdAsync(5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TEST", result.Code);
        Assert.Equal("Fixed", result.DiscountType);
        Assert.Equal(30000, result.DiscountValue);
        Assert.Equal(3, result.BranchId);
        Assert.Equal("CN3", result.BranchName);
        Assert.Equal(20, result.Quantity);
        Assert.Equal(8, result.UsedCount);
        Assert.False(result.IsActive);
    }
}
