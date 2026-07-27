using MenuGoBE.Service;
using Microsoft.Extensions.Logging;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.DeviceAuthServiceTest;

public class MarkAsScannedTests
{
    private readonly DeviceAuthService _service;

    public MarkAsScannedTests()
    {
        var loggerMock = new Mock<ILogger<DeviceAuthService>>();
        _service = new DeviceAuthService(loggerMock.Object);
    }

    [Fact]
    public void ReturnsTrue_WhenValid_NotYetScanned()
    {
        // Arrange
        var challenge = _service.CreateChallenge("token");

        // Act
        var result = _service.MarkAsScanned(challenge.ChallengeId, "user@test.com", "Nguyễn Văn A");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ReturnsFalse_WhenAlreadyScanned()
    {
        // Arrange
        var challenge = _service.CreateChallenge("token");
        _service.MarkAsScanned(challenge.ChallengeId, "user1@test.com", "User 1");

        // Act
        var result = _service.MarkAsScanned(challenge.ChallengeId, "user2@test.com", "User 2");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ReturnsFalse_WhenExpired()
    {
        // Arrange
        var challenge = _service.CreateChallenge("token");
        challenge.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);

        // Act
        var result = _service.MarkAsScanned(challenge.ChallengeId, "user@test.com", "User");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SetsEmailAndName_Correctly()
    {
        // Arrange
        var challenge = _service.CreateChallenge("token");

        // Act
        _service.MarkAsScanned(challenge.ChallengeId, "admin@restaurant.com", "Nguyễn Quản Lý");
        var scanned = _service.GetChallenge(challenge.ChallengeId);

        // Assert
        Assert.NotNull(scanned);
        Assert.True(scanned.IsScanned);
        Assert.Equal("admin@restaurant.com", scanned.ScannedByEmail);
        Assert.Equal("Nguyễn Quản Lý", scanned.ScannedByName);
    }

    [Fact]
    public void ReturnsFalse_WhenChallengeNotExists()
    {
        // Act
        var result = _service.MarkAsScanned("non-existent", "email@test.com", "Name");

        // Assert
        Assert.False(result);
    }
}
