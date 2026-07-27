using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderServiceTest;

public class GetMergedSummaryAsyncTests : OrderServiceTestBase
{
    [Fact]
    public async Task ReturnsSummary_WhenFatherExists()
    {
        var father = new Order { Id = 1, TableId = 1, Status = "Active", TotalAmount = 100000 };
        var children = new List<Order>
        {
            new Order { Id = 2, TableId = 2, Status = "Active", FatherId = 1, TotalAmount = 50000 },
            new Order { Id = 3, TableId = 3, Status = "Active", FatherId = 1, TotalAmount = 30000 }
        };

        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(father);
        _orderRepoMock.Setup(r => r.GetChildOrdersAsync(1)).ReturnsAsync(children);

        var result = await _service.GetMergedSummaryAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.FatherOrder.Id);
        Assert.Equal(2, result.ChildOrders.Count);
        Assert.Equal(180000, result.GrandTotal); // 100k + 50k + 30k
    }

    [Fact]
    public async Task ReturnsNull_WhenFatherNotExists()
    {
        _orderRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Order?)null);

        var result = await _service.GetMergedSummaryAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task ReturnsGrandTotal_OnlyFather_WhenNoChildren()
    {
        var father = new Order { Id = 1, TableId = 1, Status = "Active", TotalAmount = 150000 };
        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(father);
        _orderRepoMock.Setup(r => r.GetChildOrdersAsync(1)).ReturnsAsync(new List<Order>());

        var result = await _service.GetMergedSummaryAsync(1);

        Assert.NotNull(result);
        Assert.Empty(result.ChildOrders);
        Assert.Equal(150000, result.GrandTotal);
    }
}
