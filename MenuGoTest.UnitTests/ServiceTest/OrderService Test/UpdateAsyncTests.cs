using MenuGoBE.Dtos.Order;
using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderServiceTest;

public class UpdateAsyncTests : OrderServiceTestBase
{
    [Fact]
    public async Task ReturnsTrue_WhenExists()
    {
        var existing = new Order { Id = 1, TableId = 1, Status = "Active", TotalAmount = 100000 };
        var dto = new OrderUpdateDto { Id = 1, TableId = 2, Status = "Active", TotalAmount = 200000 };

        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync(dto);

        Assert.True(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNotExists()
    {
        var dto = new OrderUpdateDto { Id = 999, TableId = 1, Status = "Active" };
        _orderRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Order?)null);

        var result = await _service.UpdateAsync(dto);

        Assert.False(result);
    }

    [Fact]
    public async Task CallsRepoUpdateAndSave()
    {
        var existing = new Order { Id = 1, TableId = 1, Status = "Active" };
        var dto = new OrderUpdateDto { Id = 1, TableId = 1, Status = "Paid" };

        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _service.UpdateAsync(dto);

        _orderRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
        _orderRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
