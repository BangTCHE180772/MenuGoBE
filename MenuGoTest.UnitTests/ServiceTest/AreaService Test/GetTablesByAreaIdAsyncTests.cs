using AutoMapper;
using MenuGoBE.Dtos.Table;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.AreaServiceTest;

public class GetTablesByAreaIdAsyncTests
{
    private readonly Mock<IAreaRepository> _repoMock;
    private readonly Mock<ITableRepository> _tableRepoMock;
    private readonly IMapper _mapper;
    private readonly AreaService _service;

    public GetTablesByAreaIdAsyncTests()
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
    public async Task ReturnsFilteredTables_WhenAreaHasTables()
    {
        // Arrange
        var tables = new List<Table>
        {
            new Table { Id = 1, AreaId = 10, Name = "Bàn 1", Status = "Empty", IsActive = true, Area = new Area { Id = 10, Name = "Tầng 1", BranchId = 1 } },
            new Table { Id = 2, AreaId = 10, Name = "Bàn 2", Status = "Occupied", IsActive = true, Area = new Area { Id = 10, Name = "Tầng 1", BranchId = 1 } },
            new Table { Id = 3, AreaId = 20, Name = "Bàn 3", Status = "Empty", IsActive = true, Area = new Area { Id = 20, Name = "Tầng 2", BranchId = 1 } }
        };
        _tableRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(tables);

        // Act
        var result = await _service.GetTablesByAreaIdAsync(10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.Equal(10, dto.AreaId));
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenAreaHasNoTables()
    {
        // Arrange
        var tables = new List<Table>
        {
            new Table { Id = 1, AreaId = 10, Name = "Bàn 1", Status = "Empty", IsActive = true, Area = new Area { Id = 10, Name = "Tầng 1", BranchId = 1 } }
        };
        _tableRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(tables);

        // Act
        var result = await _service.GetTablesByAreaIdAsync(999);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task FiltersOnlyTablesForGivenArea()
    {
        // Arrange - 3 bàn thuộc 3 area khác nhau
        var tables = new List<Table>
        {
            new Table { Id = 1, AreaId = 1, Name = "Bàn A1", Status = "Empty", IsActive = true, Area = new Area { Id = 1, Name = "Khu A", BranchId = 1 } },
            new Table { Id = 2, AreaId = 2, Name = "Bàn B1", Status = "Empty", IsActive = true, Area = new Area { Id = 2, Name = "Khu B", BranchId = 1 } },
            new Table { Id = 3, AreaId = 3, Name = "Bàn C1", Status = "Empty", IsActive = true, Area = new Area { Id = 3, Name = "Khu C", BranchId = 1 } }
        };
        _tableRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(tables);

        // Act
        var result = await _service.GetTablesByAreaIdAsync(2);

        // Assert
        Assert.Single(result);
        Assert.Equal("Bàn B1", result[0].Name);
    }
}
