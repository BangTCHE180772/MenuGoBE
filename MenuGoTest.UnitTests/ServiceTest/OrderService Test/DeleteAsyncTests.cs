using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderServiceTest;

public class DeleteAsyncTests : OrderServiceTestBase
{
    [Fact]
    public async Task ReturnsTrue_WhenExists()
    {
        var existing = new Order { Id = 1, TableId = 1, Status = "Active" };
        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _orderRepoMock.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNotExists()
    {
        _orderRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Order?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result);
    }
}
