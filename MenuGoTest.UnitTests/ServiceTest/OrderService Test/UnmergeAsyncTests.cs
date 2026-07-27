using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderServiceTest;

public class UnmergeAsyncTests : OrderServiceTestBase
{
    [Fact]
    public async Task ReturnsTrue_ChildOrder_RemovesFatherId()
    {
        // Order is a child (has FatherId) → just set FatherId = null
        var order = new Order { Id = 2, TableId = 2, Status = "Active", FatherId = 1 };
        _orderRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.UnmergeAsync(2);

        Assert.True(result);
        _orderRepoMock.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Id == 2 && o.FatherId == null)), Times.Once);
    }

    [Fact]
    public async Task ReturnsTrue_FatherOrder_PromotesFirstChild()
    {
        // Father(id=1) with children [3, 4] → child[0] (id=3) becomes new root
        var father = new Order { Id = 1, TableId = 1, Status = "Active", FatherId = null };
        var child1 = new Order { Id = 3, TableId = 3, Status = "Active", FatherId = 1 };
        var child2 = new Order { Id = 4, TableId = 4, Status = "Active", FatherId = 1 };

        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(father);
        _orderRepoMock.Setup(r => r.GetChildOrdersAsync(1)).ReturnsAsync(new List<Order> { child1, child2 });
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.UnmergeAsync(1);

        Assert.True(result);
        // First child promoted to root
        _orderRepoMock.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Id == 3 && o.FatherId == null)), Times.Once);
    }

    [Fact]
    public async Task ReturnsTrue_FatherOrder_ReassignsOtherChildren()
    {
        var father = new Order { Id = 1, TableId = 1, Status = "Active", FatherId = null };
        var child1 = new Order { Id = 3, TableId = 3, Status = "Active", FatherId = 1 };
        var child2 = new Order { Id = 4, TableId = 4, Status = "Active", FatherId = 1 };

        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(father);
        _orderRepoMock.Setup(r => r.GetChildOrdersAsync(1)).ReturnsAsync(new List<Order> { child1, child2 });
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _service.UnmergeAsync(1);

        // child2 now points to child1 as new root
        _orderRepoMock.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Id == 4 && o.FatherId == 3)), Times.Once);
    }

    [Fact]
    public async Task ReturnsFalse_FatherWithNoChildren()
    {
        var father = new Order { Id = 1, TableId = 1, Status = "Active", FatherId = null };
        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(father);
        _orderRepoMock.Setup(r => r.GetChildOrdersAsync(1)).ReturnsAsync(new List<Order>());

        var result = await _service.UnmergeAsync(1);

        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenOrderNotExists()
    {
        _orderRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Order?)null);

        var result = await _service.UnmergeAsync(999);

        Assert.False(result);
    }
}
