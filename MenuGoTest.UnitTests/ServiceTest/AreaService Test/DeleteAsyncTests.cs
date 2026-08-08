using AutoMapper;
using MenuGoBE.Exceptions;
using MenuGoBE.Dtos.Area;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.AreaServiceTest;

public class DeleteAsyncTests
{
    private readonly Mock<IAreaRepository> _repoMock;
    private readonly Mock<ITableRepository> _tableRepoMock;
    private readonly IMapper _mapper;
    private readonly AreaService _service;

    public DeleteAsyncTests()
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
    public async Task ReturnsTrue_WhenAreaExists()
    {
        // Arrange
        var existing = new Area { Id = 1, BranchId = 1, Name = "Tầng 1", Status = "Active", IsActive = true };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _tableRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Table>());
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Area>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        Assert.False(existing.IsActive);
    }

    [Fact]
    public async Task ReturnsFalse_WhenAreaNotExists()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Area?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CallsRepoUpdateAndSave_WhenExists()
    {
        // Arrange
        var existing = new Area { Id = 1, BranchId = 1, Name = "Tầng 1", Status = "Active", IsActive = true };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _tableRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Table>());
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Area>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(1);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ThrowsException_WhenAreaHasOccupiedTables()
    {
        var existing = new Area { Id = 1, BranchId = 1, Name = "Tầng 1", Status = "Active", IsActive = true };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        
        var tables = new List<Table>
        {
            new Table { Id = 1, AreaId = 1, Status = "Empty", Name = "Bàn 1", IsActive = true }
        };
        _tableRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(tables);
        
        var activeOrder = new Order { Id = 1, TableId = 1, Status = "Active" };
        var orderRepoMock = new Mock<IOrderRepository>();
        orderRepoMock.Setup(r => r.GetActiveOrderByTableIdAsync(1)).ReturnsAsync(activeOrder);
        
        var service = new AreaService(_repoMock.Object, _tableRepoMock.Object, _mapper, orderRepoMock.Object, new Mock<IReservationRepository>().Object);

        var exception = await Assert.ThrowsAsync<MenuGoException>(() => service.DeleteAsync(1));
        Assert.Contains("đang có hóa đơn chưa thanh toán", exception.Message);
    }
    
    [Fact]
    public async Task ThrowsException_WhenAreaHasReservedTables()
    {
        var existing = new Area { Id = 1, BranchId = 1, Name = "Tầng 1", Status = "Active", IsActive = true };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        
        var tables = new List<Table>
        {
            new Table { Id = 1, AreaId = 1, Status = "Empty", Name = "Bàn 1", IsActive = true }
        };
        _tableRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(tables);
        
        var activeOrder = new Order { Id = 1, TableId = 1, Status = "Reserved" };
        var orderRepoMock = new Mock<IOrderRepository>();
        orderRepoMock.Setup(r => r.GetActiveOrderByTableIdAsync(1)).ReturnsAsync(activeOrder);
        
        var service = new AreaService(_repoMock.Object, _tableRepoMock.Object, _mapper, orderRepoMock.Object, new Mock<IReservationRepository>().Object);

        var exception = await Assert.ThrowsAsync<MenuGoException>(() => service.DeleteAsync(1));
        Assert.Contains("đang được đặt trước", exception.Message);
    }
}
