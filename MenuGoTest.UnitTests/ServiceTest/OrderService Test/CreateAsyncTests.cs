using MenuGoBE.Dtos.Order;
using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderServiceTest;

public class CreateAsyncTests : OrderServiceTestBase
{
    [Fact]
    public async Task ReturnsDto_WhenValid()
    {
        var dto = new OrderCreateDto { TableId = 1, Status = "Active", TotalAmount = 0 };
        _orderRepoMock.Setup(r => r.GetActiveOrderByTableIdAsync(1)).ReturnsAsync((Order?)null);
        _orderRepoMock.Setup(r => r.CreateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(1, result.TableId);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task ThrowsException_WhenTableHasActiveOrder()
    {
        var dto = new OrderCreateDto { TableId = 1, Status = "Active" };
        _orderRepoMock.Setup(r => r.GetActiveOrderByTableIdAsync(1)).ReturnsAsync(new Order { Id = 99, TableId = 1, Status = "Active" });

        var ex = await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateAsync(dto));
        Assert.Contains("đã có hóa đơn", ex.Message);
    }

    [Fact]
    public async Task AllowsCreation_WhenTableIdIsZero()
    {
        var dto = new OrderCreateDto { TableId = 0, Status = "Active" };
        _orderRepoMock.Setup(r => r.CreateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        // Should NOT call GetActiveOrderByTableIdAsync when TableId = 0
        _orderRepoMock.Verify(r => r.GetActiveOrderByTableIdAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task CallsRepoCreateAndSave()
    {
        var dto = new OrderCreateDto { TableId = 0, Status = "Active" };
        _orderRepoMock.Setup(r => r.CreateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _service.CreateAsync(dto);

        _orderRepoMock.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Once);
        _orderRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
