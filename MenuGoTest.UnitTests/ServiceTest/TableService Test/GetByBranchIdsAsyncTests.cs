using AutoMapper;
using MenuGoBE.Dtos.Table;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.TableServiceTest;

public class GetByBranchIdsAsyncTests
{
    private readonly Mock<ITableRepository> _repoMock;
    private readonly IMapper _mapper;
    private readonly TableService _service;

    public GetByBranchIdsAsyncTests()
    {
        _repoMock = new Mock<ITableRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new TableService(_repoMock.Object, _mapper, new Mock<MenuGoBE.Interface.Repository.IReservationRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IOrderRepository>().Object);
    }

    [Fact]
    public async Task ReturnsList_WhenBranchIdsValid()
    {
        // Arrange
        var branchIds = new List<long> { 1, 2 };
        var tables = new List<Table>
        {
            new Table { Id = 1, AreaId = 1, Name = "Bàn A1", Status = "Empty", IsActive = true, Area = new Area { Id = 1, Name = "Khu A", BranchId = 1 } },
            new Table { Id = 2, AreaId = 2, Name = "Bàn B1", Status = "Empty", IsActive = true, Area = new Area { Id = 2, Name = "Khu B", BranchId = 2 } }
        };
        _repoMock.Setup(r => r.GetByBranchIdsAsync(branchIds)).ReturnsAsync(tables);

        // Act
        var result = await _service.GetByBranchIdsAsync(branchIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenBranchIdsEmpty()
    {
        // Arrange
        var branchIds = new List<long>();
        _repoMock.Setup(r => r.GetByBranchIdsAsync(branchIds)).ReturnsAsync(new List<Table>());

        // Act
        var result = await _service.GetByBranchIdsAsync(branchIds);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReturnsList_ForMultipleBranches()
    {
        // Arrange
        var branchIds = new List<long> { 10, 20, 30 };
        var tables = new List<Table>
        {
            new Table { Id = 1, AreaId = 1, Name = "Bàn 1", Status = "Empty", IsActive = true, Area = new Area { Id = 1, Name = "CN1", BranchId = 10 } },
            new Table { Id = 2, AreaId = 2, Name = "Bàn 2", Status = "Empty", IsActive = true, Area = new Area { Id = 2, Name = "CN2", BranchId = 20 } },
            new Table { Id = 3, AreaId = 3, Name = "Bàn 3", Status = "Empty", IsActive = true, Area = new Area { Id = 3, Name = "CN3", BranchId = 30 } }
        };
        _repoMock.Setup(r => r.GetByBranchIdsAsync(branchIds)).ReturnsAsync(tables);

        // Act
        var result = await _service.GetByBranchIdsAsync(branchIds);

        // Assert
        Assert.Equal(3, result.Count);
    }
}
