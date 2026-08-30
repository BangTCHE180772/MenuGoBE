using MenuGoBE.Service;
using Microsoft.Extensions.Logging;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.DeviceAuthServiceTest;

public class CreateChallengeTests
{
    private readonly DeviceAuthService _service;

    public CreateChallengeTests()
    {
        var loggerMock = new Mock<ILogger<DeviceAuthService>>();
        _service = new DeviceAuthService(loggerMock.Object);
    }

    [Fact]
    public void ReturnsChallengeWithUniqueId()
    {
        // Act
        var result = _service.CreateChallenge("device-token-123");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ChallengeId);
        Assert.NotEmpty(result.ChallengeId);
    }

    [Fact]
    public void SetsDeviceToken_Correctly()
    {
        // Act
        var result = _service.CreateChallenge("my-token");

        // Assert
        Assert.Equal("my-token", result.DeviceToken);
    }

    [Fact]
    public void SetsExpiresAt_5MinutesFromNow()
    {
        // Act
        var before = DateTime.UtcNow.AddMinutes(5).AddSeconds(-5);
        var result = _service.CreateChallenge("token");
        var after = DateTime.UtcNow.AddMinutes(5).AddSeconds(5);

        // Assert
        Assert.InRange(result.ExpiresAt, before, after);
    }

    [Fact]
    public void CanCreateMultipleChallenges_SameToken()
    {
        // Act
        var c1 = _service.CreateChallenge("same-token");
        var c2 = _service.CreateChallenge("same-token");

        // Assert
        Assert.NotEqual(c1.ChallengeId, c2.ChallengeId);
    }

    [Fact]
    public void CreatedChallenge_IsNotScanned()
    {
        // Act
        var result = _service.CreateChallenge("token");

        // Assert
        Assert.False(result.IsScanned);
        Assert.Null(result.ScannedByEmail);
        Assert.Null(result.ScannedByName);
    }
}
