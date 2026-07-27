using MenuGoBE.Dtos.Voucher;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.VoucherServiceTest;

public class GetAllAsyncTests
{
    private readonly Mock<IVoucherRepository> _repoMock;
    private readonly VoucherService _service;

    public GetAllAsyncTests()
    {
        _repoMock = new Mock<IVoucherRepository>();
        _service = new VoucherService(_repoMock.Object);
    }

    [Fact]
    public async Task ReturnsList_WhenDataExists()
    {
        // Arrange
        var vouchers = new List<Voucher>
        {
            new Voucher { Id = 1, Code = "SALE10", Name = "Giảm 10%", DiscountType = "Percentage", DiscountValue = 10, MaxDiscount = 50000, MinOrderValue = 100000, Quantity = 100, UsedCount = 5, IsActive = true, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(30), Branch = new Branch { Name = "CN1" } },
            new Voucher { Id = 2, Code = "FIX50K", Name = "Giảm 50k", DiscountType = "Fixed", DiscountValue = 50000, MaxDiscount = 0, MinOrderValue = 200000, Quantity = 50, UsedCount = 10, IsActive = true, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(30) }
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(vouchers);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("SALE10", result[0].Code);
        Assert.Equal("FIX50K", result[1].Code);
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenNoData()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Voucher>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task MapsAllFields_IncludingBranchName()
    {
        // Arrange
        var vouchers = new List<Voucher>
        {
            new Voucher { Id = 1, Code = "V1", Name = "Test", DiscountType = "Fixed", DiscountValue = 10000, MaxDiscount = 0, MinOrderValue = 50000, BranchId = 5, Quantity = 10, UsedCount = 3, IsActive = true, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7), Branch = new Branch { Name = "Chi nhánh Quận 1" } }
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(vouchers);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        var dto = result.First();
        Assert.Equal(1, dto.Id);
        Assert.Equal("V1", dto.Code);
        Assert.Equal("Test", dto.Name);
        Assert.Equal("Fixed", dto.DiscountType);
        Assert.Equal(10000, dto.DiscountValue);
        Assert.Equal(0, dto.MaxDiscount);
        Assert.Equal(50000, dto.MinOrderValue);
        Assert.Equal(5, dto.BranchId);
        Assert.Equal("Chi nhánh Quận 1", dto.BranchName);
        Assert.Equal(10, dto.Quantity);
        Assert.Equal(3, dto.UsedCount);
        Assert.True(dto.IsActive);
    }
}
