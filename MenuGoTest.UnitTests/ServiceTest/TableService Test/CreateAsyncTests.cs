using AutoMapper;
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
        _service = new TableService(_repoMock.Object, _mapper);
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
}
