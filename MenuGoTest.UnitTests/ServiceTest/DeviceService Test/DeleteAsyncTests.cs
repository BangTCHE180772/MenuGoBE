using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.DeviceServiceTest;

public class DeleteAsyncTests
{
    private readonly Mock<IDeviceRepository> _deviceRepoMock;
    private readonly AppDbContext _context;
    private readonly DeviceService _service;

    public DeleteAsyncTests()
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
        _deviceRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Device?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.DeleteAsync(999));
        Assert.Equal("Không tìm thấy thiết bị.", ex.Message);
    }

    [Fact]
    public async Task DeletesDevice_WhenFound()
    {
        // Arrange
        var device = new Device { Id = 1, Name = "POS 1" };
        _deviceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(device);
        _deviceRepoMock.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(1);

        // Assert
        _deviceRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }
}
