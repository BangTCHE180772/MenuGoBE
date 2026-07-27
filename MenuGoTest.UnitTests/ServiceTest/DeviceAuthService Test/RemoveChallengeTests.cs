using MenuGoBE.Service;
using Microsoft.Extensions.Logging;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.DeviceAuthServiceTest;

public class RemoveChallengeTests
{
    private readonly DeviceAuthService _service;

    public RemoveChallengeTests()
    {
        var loggerMock = new Mock<ILogger<DeviceAuthService>>();
        _service = new DeviceAuthService(loggerMock.Object);
    }

    [Fact]
    public void RemovesExistingChallenge()
    {
        // Arrange
        var challenge = _service.CreateChallenge("token");
        var id = challenge.ChallengeId;

        // Act
        _service.RemoveChallenge(id);

        // Assert - should be null after removal
        var result = _service.GetChallenge(id);
        Assert.Null(result);
    }

    [Fact]
    public void DoesNotThrow_WhenNotExists()
    {
        // Act & Assert - should not throw
        var ex = Record.Exception(() => _service.RemoveChallenge("non-existent-id"));
        Assert.Null(ex);
    }

    [Fact]
    public void GetReturnsNull_AfterRemove()
    {
        // Arrange
        var c1 = _service.CreateChallenge("token1");
        var c2 = _service.CreateChallenge("token2");

        // Act
        _service.RemoveChallenge(c1.ChallengeId);

        // Assert - c1 removed, c2 still accessible
        Assert.Null(_service.GetChallenge(c1.ChallengeId));
        Assert.NotNull(_service.GetChallenge(c2.ChallengeId));
    }
}
