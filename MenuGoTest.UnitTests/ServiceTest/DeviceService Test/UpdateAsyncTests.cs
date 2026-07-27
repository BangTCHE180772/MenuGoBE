using MenuGoBE.Data;
using MenuGoBE.Dtos.Device;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.DeviceServiceTest;

public class UpdateAsyncTests
{
    private readonly Mock<IDeviceRepository> _deviceRepoMock;
    private readonly AppDbContext _context;
    private readonly DeviceService _service;

    public UpdateAsyncTests()
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
        var dto = new DeviceUpdateDto { Id = 999, Name = "New Name", DeviceType = "POS", IsActive = true };
        _deviceRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Device?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.UpdateAsync(dto));
        Assert.Equal("Không tìm thấy thiết bị.", ex.Message);
    }

    [Fact]
    public async Task UpdatesDeviceProperties_WhenFound()
    {
        // Arrange
        var device = new Device { Id = 1, Name = "Old Name", DeviceType = "Station", IsActive = false };
        var dto = new DeviceUpdateDto { Id = 1, Name = "New Name", DeviceType = "POS", IsActive = true };

        _deviceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(device);
        _deviceRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Device>())).Returns(Task.CompletedTask);

        // Act
        await _service.UpdateAsync(dto);

        // Assert
        Assert.Equal("New Name", device.Name);
        Assert.Equal("POS", device.DeviceType);
        Assert.True(device.IsActive);
        _deviceRepoMock.Verify(r => r.UpdateAsync(device), Times.Once);
    }
}
