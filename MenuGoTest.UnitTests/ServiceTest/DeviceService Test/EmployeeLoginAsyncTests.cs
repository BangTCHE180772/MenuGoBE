using MenuGoBE.Data;
using MenuGoBE.Dtos.Device;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.DeviceServiceTest;

public class EmployeeLoginAsyncTests
{
    private readonly Mock<IDeviceRepository> _deviceRepoMock;
    private readonly AppDbContext _context;
    private readonly DeviceService _service;

    public EmployeeLoginAsyncTests()
    {
        _deviceRepoMock = new Mock<IDeviceRepository>();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _service = new DeviceService(_deviceRepoMock.Object, _context);
    }

    [Fact]
    public async Task ThrowsException_WhenDeviceNotFoundOrInactive()
    {
        // Arrange
        var dto = new DeviceEmployeeLoginDto { DeviceToken = "invalid", Email = "emp@test.com" };
        _deviceRepoMock.Setup(r => r.GetByTokenAsync("invalid")).ReturnsAsync((Device?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.EmployeeLoginAsync(dto));
        Assert.Equal("Thiết bị không hợp lệ hoặc đã bị thu hồi.", ex.Message);
    }

    [Fact]
    public async Task ThrowsException_WhenAccountNotFoundOrInactive()
    {
        // Arrange
        var device = new Device { Id = 1, BranchId = 1, Token = "token", IsActive = true };
        var dto = new DeviceEmployeeLoginDto { DeviceToken = "token", Email = "notexist@test.com" };
        _deviceRepoMock.Setup(r => r.GetByTokenAsync("token")).ReturnsAsync(device);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.EmployeeLoginAsync(dto));
        Assert.Equal("Email không tồn tại hoặc tài khoản bị khóa.", ex.Message);
    }

    [Fact]
    public async Task ThrowsException_WhenNoValidContractForBranch()
    {
        // Arrange
        var device = new Device { Id = 1, BranchId = 1, Token = "token", IsActive = true };
        var account = new Account
        {
            Id = 10,
            Email = "emp@test.com",
            IsActive = true,
            Contracts = new List<Contract>() // No contracts
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var dto = new DeviceEmployeeLoginDto { DeviceToken = "token", Email = "emp@test.com" };
        _deviceRepoMock.Setup(r => r.GetByTokenAsync("token")).ReturnsAsync(device);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.EmployeeLoginAsync(dto));
        Assert.Equal("Nhân viên không thuộc chi nhánh của thiết bị này.", ex.Message);
    }
}
