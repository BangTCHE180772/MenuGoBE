using AutoMapper;
using MenuGoBE.Exceptions;
using MenuGoBE.Dtos.Table;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.TableServiceTest;

public class CreateAsyncTests
{
    private readonly Mock<ITableRepository> _repoMock;
    private readonly IMapper _mapper;
    private readonly TableService _service;

    public CreateAsyncTests()
    {
        _repoMock = new Mock<ITableRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new TableService(_repoMock.Object, _mapper, new Mock<MenuGoBE.Interface.Repository.IReservationRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IOrderRepository>().Object);
    }

    [Fact]
    public async Task ReturnsDto_WhenValid()
    {
        // Arrange
        var dto = new TableCreateDto { AreaId = 1, Name = "Bàn mới", Status = "Empty", IsActive = true };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Table>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Bàn mới", result.Name);
        Assert.Equal(1, result.AreaId);
        Assert.Equal("Empty", result.Status);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CallsCreateAndSaveChanges()
    {
        // Arrange
        var dto = new TableCreateDto { AreaId = 1, Name = "Test", Status = "Empty", IsActive = true };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Table>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.CreateAsync(dto);

        // Assert
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Table>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
    [Fact]
    public async Task ThrowsException_WhenNameIsEmpty()
    {
        var dto = new TableCreateDto { AreaId = 1, Name = "   " };
        var exception = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(dto));
        Assert.Equal("Tên bàn không được để trống.", exception.Message);
    }

    [Fact]
    public async Task ThrowsException_WhenNameAlreadyExistsAndActive()
    {
        var dto = new TableCreateDto { AreaId = 1, Name = "Bàn 1" };
        _repoMock.Setup(r => r.GetByNameAndAreaAsync("Bàn 1", 1)).ReturnsAsync(new Table { Status = "Empty", IsActive = true });
        
        var exception = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(dto));
        Assert.Equal("Đã tồn tại bàn với tên này trong khu vực.", exception.Message);
    }

    [Fact]
    public async Task RestoresTable_WhenNameMatchesDeletedTable()
    {
        var dto = new TableCreateDto { AreaId = 1, Name = "Bàn 1" };
        var existing = new Table { Id = 1, Name = "Bàn 1", AreaId = 1, Status = "Empty", IsActive = false };
        _repoMock.Setup(r => r.GetByNameAndAreaAsync("Bàn 1", 1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Table>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        
        var result = await _service.CreateAsync(dto);
        
        Assert.True(existing.IsActive);
        _repoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
    }
}
