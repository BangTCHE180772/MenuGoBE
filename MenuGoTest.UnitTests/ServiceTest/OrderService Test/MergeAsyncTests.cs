using MenuGoBE.Dtos.Order;
using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderServiceTest;

public class MergeAsyncTests : OrderServiceTestBase
{
    [Fact]
    public async Task ReturnsTrue_WhenValidMerge()
    {
        var child = new Order { Id = 2, TableId = 2, Status = "Active", FatherId = null };
        var father = new Order { Id = 1, TableId = 1, Status = "Active", FatherId = null };

        _orderRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(child);
        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(father);
        _orderRepoMock.Setup(r => r.GetChildOrdersAsync(2)).ReturnsAsync(new List<Order>());
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.MergeAsync(new OrderMergeDto { ChildOrderId = 2, FatherOrderId = 1 });

        Assert.True(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenChildNotExists()
    {
        _orderRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Order?)null);

        var result = await _service.MergeAsync(new OrderMergeDto { ChildOrderId = 999, FatherOrderId = 1 });

        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenFatherNotExists()
    {
        var child = new Order { Id = 2, TableId = 2, Status = "Active" };
        _orderRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(child);
        _orderRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Order?)null);

        var result = await _service.MergeAsync(new OrderMergeDto { ChildOrderId = 2, FatherOrderId = 999 });

        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenSameOrder()
    {
        var order = new Order { Id = 1, TableId = 1, Status = "Active" };
        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _service.MergeAsync(new OrderMergeDto { ChildOrderId = 1, FatherOrderId = 1 });

        Assert.False(result);
    }

    [Fact]
    public async Task TraversesFatherChain_FindsRootFather()
    {
        // A(id=1) → B(id=2) → C(id=3, root)
        var child = new Order { Id = 1, TableId = 1, Status = "Active" };
        var mid = new Order { Id = 2, TableId = 2, Status = "Active", FatherId = 3 };
        var root = new Order { Id = 3, TableId = 3, Status = "Active", FatherId = null };

        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(child);
        _orderRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(mid);
        _orderRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(root);
        _orderRepoMock.Setup(r => r.GetChildOrdersAsync(1)).ReturnsAsync(new List<Order>());
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.MergeAsync(new OrderMergeDto { ChildOrderId = 1, FatherOrderId = 2 });

        Assert.True(result);
        // Child should be merged to root (id=3), not mid (id=2)
        _orderRepoMock.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Id == 1 && o.FatherId == 3)), Times.Once);
    }

    [Fact]
    public async Task SyncsChildStatusWithFather()
    {
        var child = new Order { Id = 2, TableId = 2, Status = "Pending" };
        var father = new Order { Id = 1, TableId = 1, Status = "Active", FatherId = null };

        _orderRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(child);
        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(father);
        _orderRepoMock.Setup(r => r.GetChildOrdersAsync(2)).ReturnsAsync(new List<Order>());
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _service.MergeAsync(new OrderMergeDto { ChildOrderId = 2, FatherOrderId = 1 });

        _orderRepoMock.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Id == 2 && o.Status == "Active")), Times.Once);
    }

    [Fact]
    public async Task MovesGrandchildren_ToNewFather()
    {
        // child(id=2) has grandchildren(id=4, id=5), merge into father(id=1)
        var child = new Order { Id = 2, TableId = 2, Status = "Active" };
        var father = new Order { Id = 1, TableId = 1, Status = "Active", FatherId = null };
        var grandchild1 = new Order { Id = 4, TableId = 4, Status = "Active", FatherId = 2 };
        var grandchild2 = new Order { Id = 5, TableId = 5, Status = "Active", FatherId = 2 };

        _orderRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(child);
        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(father);
        _orderRepoMock.Setup(r => r.GetChildOrdersAsync(2)).ReturnsAsync(new List<Order> { grandchild1, grandchild2 });
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _service.MergeAsync(new OrderMergeDto { ChildOrderId = 2, FatherOrderId = 1 });

        // Grandchildren now point to root father (id=1)
        _orderRepoMock.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Id == 4 && o.FatherId == 1)), Times.Once);
        _orderRepoMock.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Id == 5 && o.FatherId == 1)), Times.Once);
    }

    [Fact]
    public async Task ReturnsFalse_WhenCircularMerge()
    {
        // Father(id=1) is actually child of Child(id=2) → after traverse, childId == rootFatherId
        var child = new Order { Id = 2, TableId = 2, Status = "Active", FatherId = null };
        var father = new Order { Id = 1, TableId = 1, Status = "Active", FatherId = 2 };

        _orderRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(child);
        _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(father);

        var result = await _service.MergeAsync(new OrderMergeDto { ChildOrderId = 2, FatherOrderId = 1 });

        // After traversing father chain: father(1) -> root(2) = child(2) → circular → false
        Assert.False(result);
    }
}
