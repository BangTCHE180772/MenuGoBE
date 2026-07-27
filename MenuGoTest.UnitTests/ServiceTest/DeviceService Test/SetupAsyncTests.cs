using MenuGoBE.Data;
using MenuGoBE.Dtos.Device;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.DeviceServiceTest;

public class SetupAsyncTests
{
    private readonly Mock<IDeviceRepository> _deviceRepoMock;
    private readonly AppDbContext _context;
    private readonly DeviceService _service;

    public SetupAsyncTests()
    {
        _deviceRepoMock = new Mock<IDeviceRepository>();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _service = new DeviceService(_deviceRepoMock.Object, _context);
    }

    [Fact]
    public async Task CreatesDevice_AndReturnsTokenAndInfo()
    {
        // Arrange
        var dto = new DeviceSetupDto
        {
            Name = "POS 01",
            DeviceType = "POS"
        };
        long branchId = 1;
        long accountId = 10;

        _deviceRepoMock.Setup(r => r.CreateAsync(It.IsAny<Device>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.SetupAsync(dto, branchId, accountId);

        // Assert
        Assert.NotNull(result);
        _deviceRepoMock.Verify(r => r.CreateAsync(It.Is<Device>(d =>
            d.BranchId == branchId &&
            d.Name == "POS 01" &&
            d.DeviceType == "POS" &&
            d.SetupBy == accountId &&
            d.IsActive == true &&
            !string.IsNullOrEmpty(d.Token)
        )), Times.Once);
    }
}
