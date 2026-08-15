using AutoMapper;
using MenuGoBE.Dtos.Area;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.AreaServiceTest;

public class GetAllAsyncTests
{
    private readonly Mock<IAreaRepository> _repoMock;
    private readonly Mock<ITableRepository> _tableRepoMock;
    private readonly IMapper _mapper;
    private readonly AreaService _service;

    public GetAllAsyncTests()
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
    public async Task ReturnsListOfDtos_WhenDataExists()
    {
        // Arrange
        var areas = new List<Area>
        {
            new Area { Id = 1, BranchId = 1, Name = "Tầng 1", Status = "Active", IsActive = true },
            new Area { Id = 2, BranchId = 1, Name = "Tầng 2", Status = "Active", IsActive = true },
            new Area { Id = 3, BranchId = 2, Name = "Sân vườn", Status = "Active", IsActive = true }
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(areas);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Tầng 1", result[0].Name);
        Assert.Equal("Sân vườn", result[2].Name);
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenNoData()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Area>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task CallsRepoGetAllOnce()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Area>());

        // Act
        await _service.GetAllAsync();

        // Assert
        _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }
}
