using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderServiceTest;

public class GetChildOrdersAsyncTests : OrderServiceTestBase
{
    [Fact]
    public async Task ReturnsList_WhenChildrenExist()
    {
        var children = new List<Order>
        {
            new Order { Id = 2, TableId = 2, Status = "Active", FatherId = 1 },
            new Order { Id = 3, TableId = 3, Status = "Active", FatherId = 1 }
        };
        _orderRepoMock.Setup(r => r.GetChildOrdersAsync(1)).ReturnsAsync(children);

        var result = await _service.GetChildOrdersAsync(1);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenNoChildren()
    {
        _orderRepoMock.Setup(r => r.GetChildOrdersAsync(1)).ReturnsAsync(new List<Order>());

        var result = await _service.GetChildOrdersAsync(1);

        Assert.Empty(result);
    }
}
