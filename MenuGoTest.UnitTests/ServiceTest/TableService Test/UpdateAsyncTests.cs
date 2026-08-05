using AutoMapper;
using MenuGoBE.Exceptions;
using MenuGoBE.Dtos.Table;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.TableServiceTest;

public class UpdateAsyncTests
{
    private readonly Mock<ITableRepository> _repoMock;
    private readonly IMapper _mapper;
    private readonly TableService _service;

    public UpdateAsyncTests()
    {
        _repoMock = new Mock<ITableRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new TableService(_repoMock.Object, _mapper, new Mock<MenuGoBE.Interface.Repository.IReservationRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IOrderRepository>().Object);
    }

    [Fact]
    public async Task ReturnsTrue_WhenExists()
    {
        // Arrange
        var existing = new Table { Id = 1, AreaId = 1, Name = "Old", Status = "Empty", IsActive = true, Area = new Area { Id = 1, Name = "T1", BranchId = 1 } };
        var dto = new TableUpdateDto { Id = 1, AreaId = 1, Name = "New Name", Status = "Reserved", IsActive = false };

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Table>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNotExists()
    {
        // Arrange
        var dto = new TableUpdateDto { Id = 999, AreaId = 1, Name = "Test", Status = "Empty", IsActive = true };
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Table?)null);

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task MapsFieldsCorrectly_WhenUpdated()
    {
        // Arrange
        var existing = new Table { Id = 1, AreaId = 1, Name = "Cũ", Status = "Empty", IsActive = true, Area = new Area { Id = 1, Name = "T1", BranchId = 1 } };
        var dto = new TableUpdateDto { Id = 1, AreaId = 2, Name = "Mới", Status = "Occupied", IsActive = false };

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Table>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.UpdateAsync(dto);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Table>(t =>
            t.Name == "Mới" && t.Status == "Occupied" && !t.IsActive && t.AreaId == 2
        )), Times.Once);
    }
    [Fact]
    public async Task ThrowsException_WhenNameIsEmptyOrWhitespace()
    {
        var dto = new TableUpdateDto { Id = 1, AreaId = 1, Name = "   ", Status = "Empty", IsActive = true };
        var exception = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(dto));
        Assert.Equal("Tên bàn không được để trống.", exception.Message);
    }

    [Fact]
    public async Task ThrowsException_WhenRenamingToExistingName()
    {
        var existing = new Table { Id = 1, AreaId = 1, Name = "Old", Status = "Empty", IsActive = true, Area = new Area { Id = 1, Name = "T1", BranchId = 1 } };
        var duplicate = new Table { Id = 2, AreaId = 1, Name = "New", Status = "Empty", IsActive = true, Area = new Area { Id = 1, Name = "T1", BranchId = 1 } };
        var dto = new TableUpdateDto { Id = 1, AreaId = 1, Name = "New", Status = "Empty", IsActive = true };

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.GetByNameAndAreaAsync("New", 1)).ReturnsAsync(duplicate);

        var exception = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(dto));
        Assert.Equal("Đã tồn tại bàn với tên này trong khu vực.", exception.Message);
    }

    [Fact]
    public async Task ThrowsException_WhenTableIsProtectedAndStatusOccupied()
    {
        var existing = new Table { Id = 1, AreaId = 1, Name = "Bàn 1", Status = "Empty", IsActive = true, Area = new Area { Id = 1, Name = "T1", BranchId = 1 } };
        var dto = new TableUpdateDto { Id = 1, AreaId = 1, Name = "Bàn 1", Status = "Occupied", IsActive = true };

        var reservationRepoMock = new Mock<IReservationRepository>();
        reservationRepoMock.Setup(r => r.IsTableProtectedAsync(1, 60)).ReturnsAsync(true);
        var service = new TableService(_repoMock.Object, _mapper, reservationRepoMock.Object, new Mock<IOrderRepository>().Object);

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

        var exception = await Assert.ThrowsAsync<MenuGoException>(() => service.UpdateAsync(dto));
        Assert.Equal("Bàn này đã được đặt trước trong thời gian tới. Không thể phục vụ khách vãng lai.", exception.Message);
    }
}
