using AutoMapper;
using MenuGoBE.Dtos.KitchenStation;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.KitchenStationServiceTest;

public class KitchenStationServiceTests
{
    private readonly Mock<IKitchenStationRepository> _repoMock;
    private readonly IMapper _mapper;
    private readonly KitchenStationService _service;

    public KitchenStationServiceTests()
    {
        _repoMock = new Mock<IKitchenStationRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new KitchenStationService(_repoMock.Object, _mapper);
    }

    #region GetAllAsync / GetByIdAsync / GetByBranchIdAsync

    [Fact]
    public async Task GetAllAsync_ReturnsMappedList()
    {
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<KitchenStation>
        {
            new KitchenStation { Id = 1, BranchId = 1, Name = "Bếp 1", IsActive = true },
            new KitchenStation { Id = 2, BranchId = 1, Name = "Bếp 2", IsActive = true },
        });

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Bếp 1", result[0].Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((KitchenStation?)null);

        var result = await _service.GetByIdAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new KitchenStation { Id = 1, BranchId = 5, Name = "Bếp phụ", IsActive = true });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Bếp phụ", result!.Name);
        Assert.Equal(5, result.BranchId);
    }

    [Fact]
    public async Task GetByBranchIdAsync_ReturnsOnlyThatBranchStations()
    {
        _repoMock.Setup(r => r.GetByBranchIdAsync(7)).ReturnsAsync(new List<KitchenStation>
        {
            new KitchenStation { Id = 1, BranchId = 7, Name = "Bếp 1", IsActive = true },
        });

        var result = await _service.GetByBranchIdAsync(7);

        Assert.Single(result);
        Assert.All(result, s => Assert.Equal(7, s.BranchId));
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_Throws_WhenNameEmpty()
    {
        var dto = new KitchenStationCreateDto { BranchId = 1, Name = "   " };

        await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_CreatesNewStation_WhenNoExistingWithSameName()
    {
        var dto = new KitchenStationCreateDto { BranchId = 1, Name = "Bếp mới" };

        _repoMock.Setup(r => r.GetByNameAndBranchAsync("Bếp mới", 1)).ReturnsAsync((KitchenStation?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<KitchenStation>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(dto);

        Assert.Equal("Bếp mới", result.Name);
        Assert.Equal(1, result.BranchId);
        _repoMock.Verify(r => r.CreateAsync(It.Is<KitchenStation>(k => k.Name == "Bếp mới" && k.BranchId == 1)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenActiveDuplicateExistsInSameBranch()
    {
        var dto = new KitchenStationCreateDto { BranchId = 1, Name = "Bếp 1" };
        _repoMock.Setup(r => r.GetByNameAndBranchAsync("Bếp 1", 1))
            .ReturnsAsync(new KitchenStation { Id = 5, BranchId = 1, Name = "Bếp 1", IsActive = true });

        await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(dto));
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<KitchenStation>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ReactivatesInactiveStation_WhenNameMatchesSoftDeleted()
    {
        var dto = new KitchenStationCreateDto { BranchId = 1, Name = "Bếp cũ" };
        var inactive = new KitchenStation { Id = 8, BranchId = 1, Name = "Bếp cũ", IsActive = false };
        _repoMock.Setup(r => r.GetByNameAndBranchAsync("Bếp cũ", 1)).ReturnsAsync(inactive);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<KitchenStation>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(dto);

        Assert.True(inactive.IsActive);
        Assert.Equal(8, result.Id);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<KitchenStation>()), Times.Never);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<KitchenStation>()), Times.Once);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ReturnsFalse_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((KitchenStation?)null);

        var result = await _service.UpdateAsync(new KitchenStationUpdateDto { Id = 1, BranchId = 1, Name = "X" });

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenNameEmpty()
    {
        var dto = new KitchenStationUpdateDto { Id = 1, BranchId = 1, Name = " " };

        await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenRenamingToActiveDuplicate()
    {
        var entity = new KitchenStation { Id = 1, BranchId = 1, Name = "Bếp 1", IsActive = true };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _repoMock.Setup(r => r.GetByNameAndBranchAsync("Bếp 2", 1))
            .ReturnsAsync(new KitchenStation { Id = 2, BranchId = 1, Name = "Bếp 2", IsActive = true });

        var dto = new KitchenStationUpdateDto { Id = 1, BranchId = 1, Name = "Bếp 2" };

        await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_ReturnsTrue_AndUpdatesEntity_WhenValid()
    {
        var entity = new KitchenStation { Id = 1, BranchId = 1, Name = "Bếp cũ", IsActive = true };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _repoMock.Setup(r => r.GetByNameAndBranchAsync("Bếp mới", 1)).ReturnsAsync((KitchenStation?)null);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<KitchenStation>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync(new KitchenStationUpdateDto { Id = 1, BranchId = 1, Name = "Bếp mới", IsActive = true });

        Assert.True(result);
        Assert.Equal("Bếp mới", entity.Name);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((KitchenStation?)null);

        var result = await _service.DeleteAsync(1);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenUsedByProduct()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new KitchenStation { Id = 1, BranchId = 1, Name = "Bếp 1", IsActive = true });
        _repoMock.Setup(r => r.IsUsedByAnyProductAsync(1)).ReturnsAsync(true);

        await Assert.ThrowsAsync<MenuGoException>(() => _service.DeleteAsync(1));
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<KitchenStation>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_WhenNotUsedByAnyProduct()
    {
        var entity = new KitchenStation { Id = 1, BranchId = 1, Name = "Bếp 1", IsActive = true };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _repoMock.Setup(r => r.IsUsedByAnyProductAsync(1)).ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<KitchenStation>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1);

        Assert.True(result);
        Assert.False(entity.IsActive);
    }

    #endregion
}
