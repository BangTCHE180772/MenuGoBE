using AutoMapper;
using MenuGoBE.Dtos.Table;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.TableServiceTest;

public class GetAllAsyncTests
{
    private readonly Mock<ITableRepository> _repoMock;
    private readonly IMapper _mapper;
    private readonly TableService _service;

    public GetAllAsyncTests()
    {
        _repoMock = new Mock<ITableRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new TableService(_repoMock.Object, _mapper, new Mock<MenuGoBE.Interface.Repository.IReservationRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IOrderRepository>().Object);
    }

    [Fact]
    public async Task ReturnsMappedList_WhenDataExists()
    {
        // Arrange
        var tables = new List<Table>
        {
            new Table { Id = 1, AreaId = 1, Name = "Bàn 1", Status = "Empty", IsActive = true, Area = new Area { Id = 1, Name = "Tầng 1", BranchId = 1 } },
            new Table { Id = 2, AreaId = 1, Name = "Bàn 2", Status = "Occupied", IsActive = true, Area = new Area { Id = 1, Name = "Tầng 1", BranchId = 1 } }
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(tables);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Bàn 1", result[0].Name);
        Assert.Equal("Bàn 2", result[1].Name);
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenNoData()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Table>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
