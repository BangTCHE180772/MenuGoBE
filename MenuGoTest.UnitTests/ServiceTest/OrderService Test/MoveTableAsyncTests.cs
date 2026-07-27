using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderServiceTest;

public class MoveTableAsyncTests : OrderServiceTestBase
{
    [Fact]
    public async Task ReturnsTrue_ValidMove_SwapsTableStatus()
    {
        var order = new Order { Id = 1, TableId = 10, Status = "Active" };
        var oldTable = new Table { Id = 10, Name = "Bàn 10", Status = "Occupied", AreaId = 1 };
        var newTable = new Table { Id = 20, Name = "Bàn 20", Status = "Empty", AreaId = 1 };

        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _tableRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(newTable);
        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(oldTable);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.MoveTableAsync(1, 20);

        Assert.True(result);
        Assert.Equal("Empty", oldTable.Status);
        Assert.Equal("Occupied", newTable.Status);
    }

    [Fact]
    public async Task ReturnsFalse_WhenOrderNotExists()
    {
        _orderRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Order?)null);

        var result = await _service.MoveTableAsync(999, 20);

        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenOrderNotActive()
    {
        var order = new Order { Id = 1, TableId = 10, Status = "Paid" };
        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _service.MoveTableAsync(1, 20);

        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNewTableNotEmpty()
    {
        var order = new Order { Id = 1, TableId = 10, Status = "Active" };
        var newTable = new Table { Id = 20, Name = "Bàn 20", Status = "Occupied", AreaId = 1 };

        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _tableRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(newTable);

        var result = await _service.MoveTableAsync(1, 20);

        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNewTableNotExists()
    {
        var order = new Order { Id = 1, TableId = 10, Status = "Active" };
        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _tableRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync((Table?)null);

        var result = await _service.MoveTableAsync(1, 20);

        Assert.False(result);
    }
}
