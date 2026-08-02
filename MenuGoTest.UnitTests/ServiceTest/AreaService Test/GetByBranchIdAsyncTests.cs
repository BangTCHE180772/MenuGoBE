using AutoMapper;
using MenuGoBE.Dtos.Area;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.AreaServiceTest;

public class GetByBranchIdAsyncTests
{
    private readonly Mock<IAreaRepository> _repoMock;
    private readonly Mock<ITableRepository> _tableRepoMock;
    private readonly IMapper _mapper;
    private readonly AreaService _service;

    public GetByBranchIdAsyncTests()
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
    public async Task ReturnsList_WhenBranchHasAreas()
    {
        // Arrange
        var areas = new List<Area>
        {
            new Area { Id = 1, BranchId = 10, Name = "Tầng 1", Status = "Active", IsActive = true },
            new Area { Id = 2, BranchId = 10, Name = "Tầng 2", Status = "Active", IsActive = true }
        };
        _repoMock.Setup(r => r.GetByBranchIdAsync(10)).ReturnsAsync(areas);

        // Act
        var result = await _service.GetByBranchIdAsync(10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.Equal(10, dto.BranchId));
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenBranchHasNoAreas()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByBranchIdAsync(999)).ReturnsAsync(new List<Area>());

        // Act
        var result = await _service.GetByBranchIdAsync(999);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
