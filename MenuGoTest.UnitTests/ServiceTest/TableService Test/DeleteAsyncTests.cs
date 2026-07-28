using AutoMapper;
using MenuGoBE.Dtos.Table;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.TableServiceTest;

public class DeleteAsyncTests
{
    private readonly Mock<ITableRepository> _repoMock;
    private readonly IMapper _mapper;
    private readonly TableService _service;

    public DeleteAsyncTests()
    {
        _repoMock = new Mock<ITableRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new TableService(_repoMock.Object, _mapper);
    }

    [Fact]
    public async Task ReturnsTrue_WhenExists()
    {
        // Arrange
        var existing = new Table { Id = 1, AreaId = 1, Name = "Bàn 1", Status = "Empty", IsActive = true, Area = new Area { Id = 1, Name = "T1", BranchId = 1 } };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNotExists()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Table?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CallsRepoDeleteAndSave_WhenExists()
    {
        // Arrange
        var existing = new Table { Id = 5, AreaId = 1, Name = "Bàn 5", Status = "Empty", IsActive = true, Area = new Area { Id = 1, Name = "T1", BranchId = 1 } };
        _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.DeleteAsync(5)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(5);

        // Assert
        _repoMock.Verify(r => r.DeleteAsync(5), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
