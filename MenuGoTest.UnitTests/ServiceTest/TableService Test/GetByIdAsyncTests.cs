using AutoMapper;
using MenuGoBE.Dtos.Table;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.TableServiceTest;

public class GetByIdAsyncTests
{
    private readonly Mock<ITableRepository> _repoMock;
    private readonly IMapper _mapper;
    private readonly TableService _service;

    public GetByIdAsyncTests()
    {
        _repoMock = new Mock<ITableRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new TableService(_repoMock.Object, _mapper, new Mock<MenuGoBE.Interface.Repository.IReservationRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IOrderRepository>().Object);
    }

    [Fact]
    public async Task ReturnsDto_WhenExists()
    {
        // Arrange
        var table = new Table { Id = 1, AreaId = 1, Name = "Bàn VIP 1", Status = "Empty", IsActive = true, Area = new Area { Id = 1, Name = "VIP", BranchId = 1 } };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Bàn VIP 1", result.Name);
        Assert.Equal("Empty", result.Status);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task ReturnsNull_WhenNotExists()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Table?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }
}
