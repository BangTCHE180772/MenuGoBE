using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderServiceTest;

public class GetAllAsyncTests : OrderServiceTestBase
{
    [Fact]
    public async Task ReturnsMappedList_WhenDataExists()
    {
        var orders = new List<Order>
        {
            new Order { Id = 1, TableId = 1, Status = "Active", TotalAmount = 100000 },
            new Order { Id = 2, TableId = 2, Status = "Paid", TotalAmount = 200000 }
        };
        _orderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(orders);

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenNoData()
    {
        _orderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Order>());

        var result = await _service.GetAllAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
