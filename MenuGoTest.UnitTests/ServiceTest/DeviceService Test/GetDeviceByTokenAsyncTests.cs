using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.DeviceServiceTest;

public class GetDeviceByTokenAsyncTests
{
    private readonly Mock<IDeviceRepository> _deviceRepoMock;
    private readonly AppDbContext _context;
    private readonly DeviceService _service;

    public GetDeviceByTokenAsyncTests()
    {
        _deviceRepoMock = new Mock<IDeviceRepository>();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _service = new DeviceService(_deviceRepoMock.Object, _context);
    }

    [Fact]
    public async Task ReturnsDevice_WhenTokenMatches()
    {
        // Arrange
        var device = new Device { Id = 1, Name = "POS 1", Token = "valid-token-123", BranchId = 1 };
        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDeviceByTokenAsync("valid-token-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("POS 1", result.Name);
    }

    [Fact]
    public async Task ReturnsNull_WhenTokenNotFound()
    {
        // Act
        var result = await _service.GetDeviceByTokenAsync("non-existent-token");

        // Assert
        Assert.Null(result);
    }
}
