using MenuGoBE.Service;
using Microsoft.Extensions.Logging;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.DeviceAuthServiceTest;

public class GetChallengeTests
{
    private readonly DeviceAuthService _service;

    public GetChallengeTests()
    {
        var loggerMock = new Mock<ILogger<DeviceAuthService>>();
        _service = new DeviceAuthService(loggerMock.Object);
    }

    [Fact]
    public void ReturnsChallenge_WhenValidAndNotExpired()
    {
        // Arrange
        var challenge = _service.CreateChallenge("token-1");

        // Act
        var result = _service.GetChallenge(challenge.ChallengeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(challenge.ChallengeId, result.ChallengeId);
        Assert.Equal("token-1", result.DeviceToken);
    }

    [Fact]
    public void ReturnsNull_WhenIdNotExists()
    {
        // Act
        var result = _service.GetChallenge("non-existent-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ReturnsNull_WhenExpired()
    {
        // Arrange - tạo challenge rồi sửa ExpiresAt thành quá khứ
        var challenge = _service.CreateChallenge("token");
        challenge.ExpiresAt = DateTime.UtcNow.AddMinutes(-1); // đã hết hạn

        // Act
        var result = _service.GetChallenge(challenge.ChallengeId);

        // Assert
        Assert.Null(result);
    }
}
