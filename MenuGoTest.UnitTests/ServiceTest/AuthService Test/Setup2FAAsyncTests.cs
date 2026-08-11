using MenuGoBE.Dtos.Auth;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.Extensions.Configuration;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.AuthServiceTest;

public class Setup2FAAsyncTests
{
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly AuthService _service;

    public Setup2FAAsyncTests()
    {
        _accountRepoMock = new Mock<IAccountRepository>();
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:Key"]).Returns("SuperSecretTestKeyForUnitTestThatIsLongEnough123");
        _service = new AuthService(_accountRepoMock.Object, configMock.Object, new Mock<Microsoft.Extensions.Caching.Memory.IMemoryCache>().Object, new Mock<MenuGoBE.Interface.Services.IEmailService>().Object);
    }

    [Fact]
    public async Task ThrowsArgumentException_WhenAccountNotFound()
    {
        // Arrange
        var dto = new Setup2FADto { AccountId = 999 };
        _accountRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Account?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.Setup2FAAsync(dto));
        Assert.Equal("Account not found", ex.Message);
    }

    [Fact]
    public async Task ReturnsSecretKeyAndQrCode_WhenAccountExists()
    {
        // Arrange
        var account = new Account { Id = 1, Email = "test@example.com", Name = "Test User" };
        var dto = new Setup2FADto { AccountId = 1 };
        _accountRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);

        // Act
        var result = await _service.Setup2FAAsync(dto);

        // Assert
        Assert.NotNull(result);
        var json = System.Text.Json.JsonSerializer.Serialize(result);
        Assert.Contains("secretKey", json);
        Assert.Contains("qrCodeUrl", json);
        Assert.Contains("manualEntryKey", json);
    }
}
