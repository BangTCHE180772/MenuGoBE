using MenuGoBE.Dtos.Voucher;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.VoucherServiceTest;

public class UpdateAsyncTests
{
    private readonly Mock<IVoucherRepository> _repoMock;
    private readonly VoucherService _service;

    public UpdateAsyncTests()
    {
        _repoMock = new Mock<IVoucherRepository>();
        _service = new VoucherService(_repoMock.Object);
    }

    [Fact]
    public async Task ReturnsTrue_WhenValid()
    {
        // Arrange
        var existing = new Voucher { Id = 1, Code = "OLD", Name = "Old", DiscountType = "Fixed", DiscountValue = 10000 };
        var dto = new VoucherUpdateDto { Id = 1, Code = "NEW", Name = "New", DiscountType = "Percentage", DiscountValue = 20, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7), Quantity = 50 };

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.GetByCodeAsync("NEW")).ReturnsAsync((Voucher?)null);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Voucher>())).ReturnsAsync(true);

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNotExists()
    {
        // Arrange
        var dto = new VoucherUpdateDto { Id = 999, Code = "X", Name = "X", DiscountType = "Fixed", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) };
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Voucher?)null);

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ThrowsException_WhenCodeDuplicateOtherVoucher()
    {
        // Arrange
        var existing = new Voucher { Id = 1, Code = "V1", Name = "V1" };
        var otherVoucher = new Voucher { Id = 2, Code = "V2", Name = "V2" };
        var dto = new VoucherUpdateDto { Id = 1, Code = "V2", Name = "V1 Updated", DiscountType = "Fixed", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) };

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.GetByCodeAsync("V2")).ReturnsAsync(otherVoucher);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.UpdateAsync(dto));
        Assert.Contains("đã tồn tại", ex.Message);
    }

    [Fact]
    public async Task AllowsSameCode_WhenSameVoucher()
    {
        // Arrange
        var existing = new Voucher { Id = 1, Code = "KEEP", Name = "Keep" };
        var dto = new VoucherUpdateDto { Id = 1, Code = "KEEP", Name = "Updated", DiscountType = "Fixed", DiscountValue = 5000, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7), Quantity = 10 };

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.GetByCodeAsync("KEEP")).ReturnsAsync(existing); // same voucher
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Voucher>())).ReturnsAsync(true);

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        Assert.True(result);
    }
}
