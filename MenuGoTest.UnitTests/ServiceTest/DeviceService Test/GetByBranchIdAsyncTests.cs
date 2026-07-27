using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.DeviceServiceTest;

public class GetByBranchIdAsyncTests
{
    private readonly Mock<IDeviceRepository> _deviceRepoMock;
    private readonly AppDbContext _context;
    private readonly DeviceService _service;

    public GetByBranchIdAsyncTests()
    {
        _deviceRepoMock = new Mock<IDeviceRepository>();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _service = new DeviceService(_deviceRepoMock.Object, _context);
    }

    [Fact]
    public async Task ReturnsDeviceList_ForGivenBranch()
    {
        // Arrange
        long branchId = 1;
        var devices = new List<Device>
        {
            new Device { Id = 1, BranchId = branchId, Name = "POS 1", DeviceType = "POS", IsActive = true },
            new Device { Id = 2, BranchId = branchId, Name = "Kitchen 1", DeviceType = "Kitchen", IsActive = true }
        };
        _deviceRepoMock.Setup(r => r.GetByBranchIdAsync(branchId)).ReturnsAsync(devices);

        // Act
        var result = await _service.GetByBranchIdAsync(branchId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }
}
