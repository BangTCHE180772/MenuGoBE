using MenuGoBE.Data;
using MenuGoBE.Dtos.Device;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.DeviceServiceTest;

public class ValidateAsyncTests
{
    private readonly Mock<IDeviceRepository> _deviceRepoMock;
    private readonly AppDbContext _context;
    private readonly DeviceService _service;

    public ValidateAsyncTests()
    {
        _deviceRepoMock = new Mock<IDeviceRepository>();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _service = new DeviceService(_deviceRepoMock.Object, _context);
    }

    [Fact]
    public async Task ThrowsException_WhenDeviceNotFound()
    {
        // Arrange
        var dto = new DeviceValidateDto { Token = "invalid-token" };
        _deviceRepoMock.Setup(r => r.GetByTokenAsync("invalid-token")).ReturnsAsync((Device?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.ValidateAsync(dto));
        Assert.Equal("Thiết bị không tồn tại.", ex.Message);
    }

    [Fact]
    public async Task ThrowsException_WhenDeviceInactive()
    {
        // Arrange
        var device = new Device { Id = 1, Token = "token", IsActive = false };
        var dto = new DeviceValidateDto { Token = "token" };
        _deviceRepoMock.Setup(r => r.GetByTokenAsync("token")).ReturnsAsync(device);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.ValidateAsync(dto));
        Assert.Contains("thu hồi quyền", ex.Message);
    }

    [Fact]
    public async Task UpdatesLastActiveAt_AndReturnsInfo_WhenValid()
    {
        // Arrange
        var device = new Device { Id = 1, BranchId = 2, Name = "Kitchen 1", DeviceType = "Kitchen", Token = "valid-token", IsActive = true };
        var dto = new DeviceValidateDto { Token = "valid-token" };
        _deviceRepoMock.Setup(r => r.GetByTokenAsync("valid-token")).ReturnsAsync(device);
        _deviceRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Device>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.ValidateAsync(dto);

        // Assert
        Assert.NotNull(result);
        _deviceRepoMock.Verify(r => r.UpdateAsync(It.Is<Device>(d => d.LastActiveAt != null)), Times.Once);
    }
}
