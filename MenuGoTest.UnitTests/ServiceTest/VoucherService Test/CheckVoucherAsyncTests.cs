using MenuGoBE.Dtos.Voucher;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.VoucherServiceTest;

public class CheckVoucherAsyncTests
{
    private readonly Mock<IVoucherRepository> _repoMock;
    private readonly VoucherService _service;

    public CheckVoucherAsyncTests()
    {
        _repoMock = new Mock<IVoucherRepository>();
        _service = new VoucherService(_repoMock.Object);
    }

    private Voucher CreateValidVoucher(string discountType = "Fixed", decimal discountValue = 50000, decimal maxDiscount = 0, decimal minOrderValue = 100000, long? branchId = null)
    {
        return new Voucher
        {
            Id = 1, Code = "VALID", Name = "Test Voucher",
            DiscountType = discountType, DiscountValue = discountValue,
            MaxDiscount = maxDiscount, MinOrderValue = minOrderValue,
            BranchId = branchId, Quantity = 100, UsedCount = 5, IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(30)
        };
    }

    // ============ VALIDATION CASES (🔴) ============

    [Fact]
    public async Task ReturnsInvalid_WhenCodeNotFound()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByCodeAsync("NOTEXIST")).ReturnsAsync((Voucher?)null);

        // Act
        var result = await _service.CheckVoucherAsync("NOTEXIST", 1, 200000);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("không tồn tại", result.Message);
    }

    [Fact]
    public async Task ReturnsInvalid_WhenInactive()
    {
        // Arrange
        var voucher = CreateValidVoucher();
        voucher.IsActive = false;
        _repoMock.Setup(r => r.GetByCodeAsync("VALID")).ReturnsAsync(voucher);

        // Act
        var result = await _service.CheckVoucherAsync("VALID", 1, 200000);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("bị khóa", result.Message);
    }

    [Fact]
    public async Task ReturnsInvalid_WhenBeforeStartDate()
    {
        // Arrange
        var voucher = CreateValidVoucher();
        voucher.StartDate = DateTime.UtcNow.AddDays(5); // chưa tới ngày
        _repoMock.Setup(r => r.GetByCodeAsync("VALID")).ReturnsAsync(voucher);

        // Act
        var result = await _service.CheckVoucherAsync("VALID", 1, 200000);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("thời gian", result.Message);
    }

    [Fact]
    public async Task ReturnsInvalid_WhenAfterEndDate()
    {
        // Arrange
        var voucher = CreateValidVoucher();
        voucher.EndDate = DateTime.UtcNow.AddDays(-1); // đã quá hạn
        _repoMock.Setup(r => r.GetByCodeAsync("VALID")).ReturnsAsync(voucher);

        // Act
        var result = await _service.CheckVoucherAsync("VALID", 1, 200000);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("thời gian", result.Message);
    }

    [Fact]
    public async Task ReturnsInvalid_WhenQuantityExhausted()
    {
        // Arrange
        var voucher = CreateValidVoucher();
        voucher.Quantity = 10;
        voucher.UsedCount = 10; // đã dùng hết
        _repoMock.Setup(r => r.GetByCodeAsync("VALID")).ReturnsAsync(voucher);

        // Act
        var result = await _service.CheckVoucherAsync("VALID", 1, 200000);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("hết lượt", result.Message);
    }

    [Fact]
    public async Task ReturnsInvalid_WhenWrongBranch()
    {
        // Arrange
        var voucher = CreateValidVoucher(branchId: 5); // chỉ áp dụng branch 5
        _repoMock.Setup(r => r.GetByCodeAsync("VALID")).ReturnsAsync(voucher);

        // Act
        var result = await _service.CheckVoucherAsync("VALID", 10, 200000); // branch 10

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("chi nhánh", result.Message);
    }

    [Fact]
    public async Task ReturnsInvalid_WhenBelowMinOrder()
    {
        // Arrange
        var voucher = CreateValidVoucher(minOrderValue: 200000);
        _repoMock.Setup(r => r.GetByCodeAsync("VALID")).ReturnsAsync(voucher);

        // Act
        var result = await _service.CheckVoucherAsync("VALID", 1, 150000); // chưa đạt 200k

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("tối thiểu", result.Message);
    }

    // ============ HAPPY PATH (🟢) ============

    [Fact]
    public async Task ReturnsValid_FixedDiscount_CorrectAmount()
    {
        // Arrange
        var voucher = CreateValidVoucher(discountType: "Fixed", discountValue: 50000, minOrderValue: 100000);
        _repoMock.Setup(r => r.GetByCodeAsync("VALID")).ReturnsAsync(voucher);

        // Act
        var result = await _service.CheckVoucherAsync("VALID", 1, 300000);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(50000, result.DiscountAmount);
        Assert.Contains("thành công", result.Message);
    }

    [Fact]
    public async Task ReturnsValid_PercentageDiscount_CorrectAmount()
    {
        // Arrange - 10% trên 200k = 20k
        var voucher = CreateValidVoucher(discountType: "Percentage", discountValue: 10, maxDiscount: 0, minOrderValue: 100000);
        _repoMock.Setup(r => r.GetByCodeAsync("VALID")).ReturnsAsync(voucher);

        // Act
        var result = await _service.CheckVoucherAsync("VALID", 1, 200000);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(20000, result.DiscountAmount);
    }

    // ============ EDGE CASES (🟡) ============

    [Fact]
    public async Task ReturnsValid_PercentageDiscount_CappedByMaxDiscount()
    {
        // Arrange - 10% trên 2M = 200k, nhưng maxDiscount = 100k → cap ở 100k
        var voucher = CreateValidVoucher(discountType: "Percentage", discountValue: 10, maxDiscount: 100000, minOrderValue: 100000);
        _repoMock.Setup(r => r.GetByCodeAsync("VALID")).ReturnsAsync(voucher);

        // Act
        var result = await _service.CheckVoucherAsync("VALID", 1, 2000000);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(100000, result.DiscountAmount);
    }

    [Fact]
    public async Task ReturnsValid_DiscountExceedsTotal_CapsAtTotal()
    {
        // Arrange - giảm 100k nhưng đơn chỉ 80k → cap ở 80k
        var voucher = CreateValidVoucher(discountType: "Fixed", discountValue: 100000, minOrderValue: 50000);
        _repoMock.Setup(r => r.GetByCodeAsync("VALID")).ReturnsAsync(voucher);

        // Act
        var result = await _service.CheckVoucherAsync("VALID", 1, 80000);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(80000, result.DiscountAmount);
    }

    [Fact]
    public async Task ReturnsValid_NullBranch_AppliesToAllBranches()
    {
        // Arrange - BranchId = null → apply tất cả
        var voucher = CreateValidVoucher(branchId: null);
        _repoMock.Setup(r => r.GetByCodeAsync("VALID")).ReturnsAsync(voucher);

        // Act
        var result = await _service.CheckVoucherAsync("VALID", 99, 200000);

        // Assert
        Assert.True(result.IsValid);
    }
}
