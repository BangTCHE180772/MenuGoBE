using AutoMapper;
using MenuGoBE.Exceptions;
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
        _service = new AreaService(_repoMock.Object, _tableRepoMock.Object, _mapper, new Mock<MenuGoBE.Interface.Repository.IOrderRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IReservationRepository>().Object);
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
    [Fact]
    public async Task ThrowsException_WhenNameIsEmptyOrWhitespace()
    {
        var dto = new AreaCreateDto { BranchId = 1, Name = "   ", Status = "Active", IsActive = true };
        var exception = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(dto));
        Assert.Equal("Tên khu vực không được để trống.", exception.Message);
    }

    [Fact]
    public async Task ThrowsException_WhenNameAlreadyExistsAndActive()
    {
        var dto = new AreaCreateDto { BranchId = 1, Name = "Khu 1", Status = "Active", IsActive = true };
        _repoMock.Setup(r => r.GetByNameAndBranchAsync("Khu 1", 1)).ReturnsAsync(new Area { Status = "Active", IsActive = true });
        
        var exception = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(dto));
        Assert.Equal("Đã tồn tại khu vực với tên này trong chi nhánh.", exception.Message);
    }

    [Fact]
    public async Task RestoresArea_WhenNameMatchesDeletedArea()
    {
        var dto = new AreaCreateDto { BranchId = 1, Name = "Khu 1", Status = "Active", IsActive = true };
        var existing = new Area { Id = 1, BranchId = 1, Name = "Khu 1", Status = "Active", IsActive = false };
        
        _repoMock.Setup(r => r.GetByNameAndBranchAsync("Khu 1", 1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Area>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        
        var result = await _service.CreateAsync(dto);
        
        Assert.True(existing.IsActive);
        _repoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
    }
}
