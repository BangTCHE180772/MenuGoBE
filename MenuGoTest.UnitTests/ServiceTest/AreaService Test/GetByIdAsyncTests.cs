using AutoMapper;
using MenuGoBE.Dtos.Area;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.AreaServiceTest;

public class GetByIdAsyncTests
{
    private readonly Mock<IAreaRepository> _repoMock;
    private readonly Mock<ITableRepository> _tableRepoMock;
    private readonly IMapper _mapper;
    private readonly AreaService _service;

    public GetByIdAsyncTests()
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
    public async Task ReturnsDto_WhenIdExists()
    {
        // Arrange
        var area = new Area { Id = 1, BranchId = 1, Name = "Tầng 1", Status = "Active", IsActive = true };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(area);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Tầng 1", result.Name);
        Assert.Equal("Active", result.Status);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task ReturnsNull_WhenIdNotExists()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Area?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ReturnsNull_WhenIdIsZero()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(0)).ReturnsAsync((Area?)null);

        // Act
        var result = await _service.GetByIdAsync(0);

        // Assert
        Assert.Null(result);
    }
}
