using MenuGoBE.Dtos.Auth;
using MenuGoBE.Helpers;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.Extensions.Configuration;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.AuthServiceTest;

public class LoginAsyncTests
{
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly AuthService _service;

    public LoginAsyncTests()
    {
        _accountRepoMock = new Mock<IAccountRepository>();
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:Key"]).Returns("SuperSecretTestKeyForUnitTestThatIsLongEnough123");
        configMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        configMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _service = new AuthService(_accountRepoMock.Object, configMock.Object, new Mock<Microsoft.Extensions.Caching.Memory.IMemoryCache>().Object, new Mock<MenuGoBE.Interface.Services.IEmailService>().Object);
    }

    [Fact]
    public async Task ThrowsUnauthorized_WhenEmailNotFound()
    {
        var dto = new LoginDto { Email = "notexist@test.com", Password = "123456" };
        _accountRepoMock.Setup(r => r.GetAccountByEmailAsync("notexist@test.com")).ReturnsAsync((Account?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.LoginAsync(dto));
    }

    [Fact]
    public async Task ThrowsUnauthorized_WhenPasswordWrong()
    {
        var account = new Account { Id = 1, Email = "test@test.com", HashedPassword = HashHelper.HashPassword("correct") };
        var dto = new LoginDto { Email = "test@test.com", Password = "wrong" };
        _accountRepoMock.Setup(r => r.GetAccountByEmailAsync("test@test.com")).ReturnsAsync(account);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.LoginAsync(dto));
    }

    [Fact]
    public async Task ReturnsToken_WhenLoginSuccess()
    {
        var account = new Account { Id = 1, Email = "test@test.com", HashedPassword = HashHelper.HashPassword("123456"), Name = "Test User" };
        var accountWithRoles = new Account { Id = 1, Email = "test@test.com", HashedPassword = HashHelper.HashPassword("123456"), Name = "Test User", Contracts = new List<Contract>() };
        var dto = new LoginDto { Email = "test@test.com", Password = "123456" };
        _accountRepoMock.Setup(r => r.GetAccountByEmailAsync("test@test.com")).ReturnsAsync(account);
        _accountRepoMock.Setup(r => r.GetAccountWithRolesAsync(1)).ReturnsAsync(accountWithRoles);

        var result = await _service.LoginAsync(dto);

        var json = System.Text.Json.JsonSerializer.Serialize(result);
        Assert.Contains("success", json);
    }
}
