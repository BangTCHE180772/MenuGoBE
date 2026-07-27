using AutoMapper;
using MenuGoBE.Dtos.Area;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.AreaServiceTest;

public class CreateAsyncTests
{
    private readonly Mock<IAreaRepository> _repoMock;
    private readonly Mock<ITableRepository> _tableRepoMock;
    private readonly IMapper _mapper;
    private readonly AreaService _service;

    public CreateAsyncTests()
    {
        _repoMock = new Mock<IAreaRepository>();
        _tableRepoMock = new Mock<ITableRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new AreaService(_repoMock.Object, _tableRepoMock.Object, _mapper);
    }

    [Fact]
    public async Task ReturnsDto_WhenDtoIsValid()
    {
        // Arrange
        var dto = new AreaCreateDto
        {
            BranchId = 1,
            Name = "Khu vực mới",
            Status = "Active",
            IsActive = true
        };

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Area>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Khu vực mới", result.Name);
        Assert.Equal(1, result.BranchId);
        Assert.Equal("Active", result.Status);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CallsRepoCreateAndSave()
    {
        // Arrange
        var dto = new AreaCreateDto { BranchId = 1, Name = "Test", Status = "Active", IsActive = true };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Area>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.CreateAsync(dto);

        // Assert
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Area>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
