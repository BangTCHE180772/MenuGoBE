using MenuGoBE.Dtos.Voucher;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.VoucherServiceTest;

public class CreateAsyncTests
{
    private readonly Mock<IVoucherRepository> _repoMock;
    private readonly VoucherService _service;

    public CreateAsyncTests()
    {
        _repoMock = new Mock<IVoucherRepository>();
        _service = new VoucherService(_repoMock.Object);
    }

    [Fact]
    public async Task ReturnsDto_WhenValid()
    {
        // Arrange
        var dto = new VoucherCreateDto
        {
            Code = "NEW50", Name = "Giảm 50k", DiscountType = "Fixed",
            DiscountValue = 50000, MaxDiscount = 0, MinOrderValue = 100000,
            StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30),
            Quantity = 100, IsActive = true
        };

        _repoMock.Setup(r => r.GetByCodeAsync("NEW50")).ReturnsAsync((Voucher?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Voucher>()))
            .ReturnsAsync((Voucher v) => { v.Id = 1; return v; });
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Voucher
        {
            Id = 1, Code = "NEW50", Name = "Giảm 50k", DiscountType = "Fixed",
            DiscountValue = 50000, MaxDiscount = 0, MinOrderValue = 100000,
            StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30),
            Quantity = 100, UsedCount = 0, IsActive = true
        });

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NEW50", result.Code);
        Assert.Equal("Giảm 50k", result.Name);
    }

    [Fact]
    public async Task ThrowsException_WhenCodeDuplicate()
    {
        // Arrange
        var dto = new VoucherCreateDto { Code = "EXIST", Name = "Test", DiscountType = "Fixed", DiscountValue = 10000, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7), Quantity = 10 };
        _repoMock.Setup(r => r.GetByCodeAsync("EXIST")).ReturnsAsync(new Voucher { Id = 99, Code = "EXIST" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.CreateAsync(dto));
        Assert.Contains("đã tồn tại", ex.Message);
    }

    [Fact]
    public async Task ConvertsCodeToUpperCase()
    {
        // Arrange
        var dto = new VoucherCreateDto
        {
            Code = "lowercase", Name = "Test", DiscountType = "Fixed",
            DiscountValue = 10000, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7), Quantity = 10
        };

        _repoMock.Setup(r => r.GetByCodeAsync("lowercase")).ReturnsAsync((Voucher?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Voucher>()))
            .ReturnsAsync((Voucher v) => { v.Id = 1; return v; });
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Voucher
        {
            Id = 1, Code = "LOWERCASE", Name = "Test", DiscountType = "Fixed",
            DiscountValue = 10000, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7),
            Quantity = 10, UsedCount = 0, IsActive = true
        });

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert - verify repo was called with uppercase code
        _repoMock.Verify(r => r.CreateAsync(It.Is<Voucher>(v => v.Code == "LOWERCASE")), Times.Once);
    }

    [Fact]
    public async Task SetsUsedCountToZero()
    {
        // Arrange
        var dto = new VoucherCreateDto
        {
            Code = "NEW", Name = "Test", DiscountType = "Fixed",
            DiscountValue = 10000, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7), Quantity = 10
        };

        _repoMock.Setup(r => r.GetByCodeAsync("NEW")).ReturnsAsync((Voucher?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Voucher>()))
            .ReturnsAsync((Voucher v) => { v.Id = 1; return v; });
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Voucher
        {
            Id = 1, Code = "NEW", Name = "Test", DiscountType = "Fixed",
            DiscountValue = 10000, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7),
            Quantity = 10, UsedCount = 0, IsActive = true
        });

        // Act
        await _service.CreateAsync(dto);

        // Assert
        _repoMock.Verify(r => r.CreateAsync(It.Is<Voucher>(v => v.UsedCount == 0)), Times.Once);
    }

    [Fact]
    public async Task ConvertsDateToUtc()
    {
        // Arrange
        var localStart = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Local);
        var localEnd = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Local);
        var dto = new VoucherCreateDto
        {
            Code = "UTC", Name = "Test", DiscountType = "Fixed",
            DiscountValue = 10000, StartDate = localStart, EndDate = localEnd, Quantity = 10
        };

        _repoMock.Setup(r => r.GetByCodeAsync("UTC")).ReturnsAsync((Voucher?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Voucher>()))
            .ReturnsAsync((Voucher v) => { v.Id = 1; return v; });
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Voucher
        {
            Id = 1, Code = "UTC", Name = "Test", DiscountType = "Fixed",
            DiscountValue = 10000, StartDate = localStart.ToUniversalTime(), EndDate = localEnd.ToUniversalTime(),
            Quantity = 10, UsedCount = 0, IsActive = true
        });

        // Act
        await _service.CreateAsync(dto);

        // Assert
        _repoMock.Verify(r => r.CreateAsync(It.Is<Voucher>(v =>
            v.StartDate.Kind == DateTimeKind.Utc && v.EndDate.Kind == DateTimeKind.Utc
        )), Times.Once);
    }
}
