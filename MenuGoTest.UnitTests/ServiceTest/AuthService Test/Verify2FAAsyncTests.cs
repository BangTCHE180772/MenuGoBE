using MenuGoBE.Dtos.Auth;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.Extensions.Configuration;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.AuthServiceTest;

public class Verify2FAAsyncTests
{
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly AuthService _service;

    public Verify2FAAsyncTests()
    {
        _accountRepoMock = new Mock<IAccountRepository>();
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:Key"]).Returns("SuperSecretTestKeyForUnitTestThatIsLongEnough123");
        configMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        configMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _service = new AuthService(_accountRepoMock.Object, configMock.Object);
    }

    [Fact]
    public async Task ThrowsArgumentException_WhenAccountNotFound()
    {
        // Arrange
        var dto = new Verify2FADto { AccountId = 999, Code = "123456" };
        _accountRepoMock.Setup(r => r.GetAccountWithRolesAsync(999)).ReturnsAsync((Account?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.Verify2FAAsync(dto));
        Assert.Equal("Account not found", ex.Message);
    }

    [Fact]
    public async Task ThrowsUnauthorizedException_WhenPinIsInvalid()
    {
        // Arrange
        var account = new Account { Id = 1, Email = "test@example.com", Name = "Test User", TwoFACode = "SOMEKEY" };
        var dto = new Verify2FADto { AccountId = 1, Code = "000000" };
        _accountRepoMock.Setup(r => r.GetAccountWithRolesAsync(1)).ReturnsAsync(account);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.Verify2FAAsync(dto));
        Assert.Equal("Mã xác thực không chính xác.", ex.Message);
    }
}
