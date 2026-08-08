using AutoMapper;
using MenuGoBE.Exceptions;
using MenuGoBE.Dtos.Table;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.TableServiceTest;

public class DeleteAsyncTests
{
    private readonly Mock<ITableRepository> _repoMock;
    private readonly IMapper _mapper;
    private readonly TableService _service;

    public DeleteAsyncTests()
    {
        _repoMock = new Mock<ITableRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new TableService(_repoMock.Object, _mapper, new Mock<MenuGoBE.Interface.Repository.IReservationRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IOrderRepository>().Object);
    }

    [Fact]
    public async Task ReturnsTrue_WhenExists()
    {
        // Arrange
        var existing = new Table { Id = 1, AreaId = 1, Name = "Bàn 1", Status = "Empty", IsActive = true, Area = new Area { Id = 1, Name = "T1", BranchId = 1 } };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Table>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        Assert.False(existing.IsActive);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNotExists()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Table?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CallsRepoUpdateAndSave_WhenExists()
    {
        // Arrange
        var existing = new Table { Id = 5, AreaId = 1, Name = "Bàn 5", Status = "Empty", IsActive = true, Area = new Area { Id = 1, Name = "T1", BranchId = 1 } };
        _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Table>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(5);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
    [Fact]
    public async Task ThrowsException_WhenTableHasActiveOrders()
    {
        var existing = new Table { Id = 1, AreaId = 1, Name = "Bàn 1", Status = "Empty", IsActive = true, Area = new Area { Id = 1, Name = "T1", BranchId = 1 } };
        var activeOrder = new Order { Id = 1, TableId = 1, Status = "Active" };

        var orderRepoMock = new Mock<IOrderRepository>();
        orderRepoMock.Setup(r => r.GetActiveOrderByTableIdAsync(1)).ReturnsAsync(activeOrder);
        var service = new TableService(_repoMock.Object, _mapper, new Mock<IReservationRepository>().Object, orderRepoMock.Object);

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

        var exception = await Assert.ThrowsAsync<MenuGoException>(() => service.DeleteAsync(1));
        Assert.Contains("đang có hóa đơn chưa thanh toán", exception.Message);
    }

    [Fact]
    public async Task ThrowsException_WhenTableIsReserved()
    {
        var existing = new Table { Id = 1, AreaId = 1, Name = "Bàn 1", Status = "Empty", IsActive = true, Area = new Area { Id = 1, Name = "T1", BranchId = 1 } };
        var activeOrder = new Order { Id = 1, TableId = 1, Status = "Reserved" };

        var orderRepoMock = new Mock<IOrderRepository>();
        orderRepoMock.Setup(r => r.GetActiveOrderByTableIdAsync(1)).ReturnsAsync(activeOrder);
        var service = new TableService(_repoMock.Object, _mapper, new Mock<IReservationRepository>().Object, orderRepoMock.Object);

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

        var exception = await Assert.ThrowsAsync<MenuGoException>(() => service.DeleteAsync(1));
        Assert.Contains("bàn đang được đặt trước", exception.Message);
    }
}
