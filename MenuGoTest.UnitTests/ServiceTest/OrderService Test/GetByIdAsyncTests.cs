using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderServiceTest;

public class GetByIdAsyncTests : OrderServiceTestBase
{
    [Fact]
    public async Task ReturnsDto_WhenExists()
    {
        var order = new Order { Id = 1, TableId = 1, Status = "Active", TotalAmount = 100000 };
        _orderRepoMock.Setup(r => r.GetOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task ReturnsNull_WhenNotExists()
    {
        _orderRepoMock.Setup(r => r.GetOrderByIdWithDetailsAsync(999)).ReturnsAsync((Order?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }
}
