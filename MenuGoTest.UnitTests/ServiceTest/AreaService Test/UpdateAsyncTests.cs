using AutoMapper;
using MenuGoBE.Exceptions;
using MenuGoBE.Dtos.Area;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.AreaServiceTest;

public class UpdateAsyncTests
{
    private readonly Mock<IAreaRepository> _repoMock;
    private readonly Mock<ITableRepository> _tableRepoMock;
    private readonly IMapper _mapper;
    private readonly AreaService _service;

    public UpdateAsyncTests()
    {
        _repoMock = new Mock<IAreaRepository>();
        _tableRepoMock = new Mock<ITableRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new AreaService(_repoMock.Object, _tableRepoMock.Object, _mapper, new Mock<MenuGoBE.Interface.Repository.IOrderRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IReservationRepository>().Object);
    }

    [Fact]
    public async Task ReturnsTrue_WhenAreaExists()
    {
        // Arrange
        var existing = new Area { Id = 1, BranchId = 1, Name = "Old Name", Status = "Active", IsActive = true };
        var dto = new AreaUpdateDto { Id = 1, BranchId = 1, Name = "New Name", Status = "Inactive", IsActive = false };

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Area>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenAreaNotExists()
    {
        // Arrange
        var dto = new AreaUpdateDto { Id = 999, BranchId = 1, Name = "Test", Status = "Active", IsActive = true };
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Area?)null);

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CallsMapperAndRepoUpdate_WhenExists()
    {
        // Arrange
        var existing = new Area { Id = 1, BranchId = 1, Name = "Old", Status = "Active", IsActive = true };
        var dto = new AreaUpdateDto { Id = 1, BranchId = 1, Name = "Updated", Status = "Active", IsActive = true };

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Area>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.UpdateAsync(dto);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Area>(a => a.Name == "Updated")), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ThrowsException_WhenNameIsEmptyOrWhitespace()
    {
        var dto = new AreaUpdateDto { Id = 1, BranchId = 1, Name = "   ", Status = "Active", IsActive = true };
        var exception = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(dto));
        Assert.Equal("Tên khu vực không được để trống.", exception.Message);
    }

    [Fact]
    public async Task ThrowsException_WhenRenamingToExistingName()
    {
        var existing = new Area { Id = 1, BranchId = 1, Name = "Old", Status = "Active", IsActive = true };
        var duplicate = new Area { Id = 2, BranchId = 1, Name = "New", Status = "Active", IsActive = true };
        var dto = new AreaUpdateDto { Id = 1, BranchId = 1, Name = "New", Status = "Active", IsActive = true };

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.GetByNameAndBranchAsync("New", 1)).ReturnsAsync(duplicate);

        var exception = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(dto));
        Assert.Equal("Đã tồn tại khu vực với tên này trong chi nhánh.", exception.Message);
    }
}
