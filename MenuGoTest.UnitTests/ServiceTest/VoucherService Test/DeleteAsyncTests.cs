using MenuGoBE.Interface.Repository;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.VoucherServiceTest;

public class DeleteAsyncTests
{
    private readonly Mock<IVoucherRepository> _repoMock;
    private readonly VoucherService _service;

    public DeleteAsyncTests()
    {
        _repoMock = new Mock<IVoucherRepository>();
        _service = new VoucherService(_repoMock.Object);
    }

    [Fact]
    public async Task ReturnsTrue_WhenRepoReturnsTrue()
    {
        // Arrange
        _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenRepoReturnsFalse()
    {
        // Arrange
        _repoMock.Setup(r => r.DeleteAsync(999)).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
    }
}
