using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderServiceTest;

public class GetTableOrderSummaryAsyncTests : OrderServiceTestBase
{
    [Fact]
    public async Task ReturnsEmptySummary_WhenNoActiveOrder()
    {
        var table = new Table { Id = 10, Name = "Bàn 10", Status = "Empty", AreaId = 1, Area = new Area { Name = "T1", BranchId = 1 } };
        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _orderRepoMock.Setup(r => r.GetActiveOrderByTableIdAsync(10)).ReturnsAsync((Order?)null);

        var result = await _service.GetTableOrderSummaryAsync(10);

        Assert.Equal(10, result.TableId);
        Assert.Equal("Bàn 10", result.TableName);
        Assert.Empty(result.Items);
        Assert.Null(result.ActiveOrderId);
    }

    [Fact]
    public async Task ReturnsItems_WhenActiveOrderExists()
    {
        var table = new Table { Id = 10, Name = "Bàn 10", Status = "Occupied", AreaId = 1, Area = new Area { Name = "T1", BranchId = 1 } };
        var order = new Order
        {
            Id = 1, TableId = 10, Status = "Active", FatherId = null,
            Table = table,
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { Id = 1, OrderId = 1, Quantity = 2, Price = 50000, Status = "Confirmed", CookingStatus = "Waiting", CreatedAt = DateTime.UtcNow, Product = new Product { Name = "Phở" } }
            }
        };

        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _orderRepoMock.Setup(r => r.GetActiveOrderByTableIdAsync(10)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order>());

        var result = await _service.GetTableOrderSummaryAsync(10);

        Assert.Equal(1, result.ActiveOrderId);
        Assert.Single(result.Items);
        Assert.Equal("Phở", result.Items[0].ProductName);
    }

    [Fact]
    public async Task ExcludesCancelledItems()
    {
        var table = new Table { Id = 10, Name = "Bàn 10", Status = "Occupied", AreaId = 1, Area = new Area { Name = "T1", BranchId = 1 } };
        var order = new Order
        {
            Id = 1, TableId = 10, Status = "Active", FatherId = null,
            Table = table,
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { Id = 1, Quantity = 2, Price = 50000, Status = "Confirmed", CookingStatus = "Waiting", CreatedAt = DateTime.UtcNow, Product = new Product { Name = "Phở" } },
                new OrderDetail { Id = 2, Quantity = 1, Price = 30000, Status = "Cancelled", CookingStatus = "Waiting", CreatedAt = DateTime.UtcNow, Product = new Product { Name = "Trà đá" } }
            }
        };

        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _orderRepoMock.Setup(r => r.GetActiveOrderByTableIdAsync(10)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order>());

        var result = await _service.GetTableOrderSummaryAsync(10);

        Assert.Single(result.Items);
        Assert.Equal("Phở", result.Items[0].ProductName);
    }

    [Fact]
    public async Task CountsPendingConfirm_AndReadyToServe()
    {
        var table = new Table { Id = 10, Name = "Bàn 10", Status = "Occupied", AreaId = 1, Area = new Area { Name = "T1", BranchId = 1 } };
        var order = new Order
        {
            Id = 1, TableId = 10, Status = "Active", FatherId = null,
            Table = table,
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { Id = 1, Quantity = 1, Price = 50000, Status = "CustomerPending", CookingStatus = "Waiting", CreatedAt = DateTime.UtcNow, Product = new Product { Name = "Item 1" } },
                new OrderDetail { Id = 2, Quantity = 1, Price = 30000, Status = "CustomerPending", CookingStatus = "Waiting", CreatedAt = DateTime.UtcNow, Product = new Product { Name = "Item 2" } },
                new OrderDetail { Id = 3, Quantity = 1, Price = 20000, Status = "Confirmed", CookingStatus = "Ready", CreatedAt = DateTime.UtcNow, Product = new Product { Name = "Item 3" } }
            }
        };

        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _orderRepoMock.Setup(r => r.GetActiveOrderByTableIdAsync(10)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order>());

        var result = await _service.GetTableOrderSummaryAsync(10);

        Assert.Equal(2, result.PendingConfirmCount);
        Assert.Equal(1, result.ReadyToServeCount);
    }

    [Fact]
    public async Task IncludesChildOrderItems_WhenMerged()
    {
        var table = new Table { Id = 10, Name = "Bàn 10", Status = "Occupied", AreaId = 1, Area = new Area { Name = "T1", BranchId = 1 } };
        var childTable = new Table { Id = 20, Name = "Bàn 20", Status = "Occupied", AreaId = 1 };
        var order = new Order
        {
            Id = 1, TableId = 10, Status = "Active", FatherId = null,
            Table = table,
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { Id = 1, OrderId = 1, Quantity = 1, Price = 50000, Status = "Confirmed", CookingStatus = "Waiting", CreatedAt = DateTime.UtcNow, Product = new Product { Name = "Phở" } }
            }
        };
        var childOrder = new Order
        {
            Id = 2, TableId = 20, Status = "Active", FatherId = 1,
            Table = childTable,
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { Id = 2, OrderId = 2, Quantity = 1, Price = 30000, Status = "Confirmed", CookingStatus = "Cooking", CreatedAt = DateTime.UtcNow, Product = new Product { Name = "Bún bò" } }
            }
        };

        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _orderRepoMock.Setup(r => r.GetActiveOrderByTableIdAsync(10)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order> { childOrder });

        var result = await _service.GetTableOrderSummaryAsync(10);

        Assert.Equal(2, result.Items.Count);
        Assert.True(result.IsMerged);
    }

    [Fact]
    public async Task SetsIsMerged_WhenChildOrder()
    {
        // Table has a child order (FatherId != null)
        var table = new Table { Id = 10, Name = "Bàn 10", Status = "Occupied", AreaId = 1, Area = new Area { Name = "T1", BranchId = 1 } };
        var childOrder = new Order
        {
            Id = 2, TableId = 10, Status = "Active", FatherId = 1,
            Table = table,
            OrderDetails = new List<OrderDetail>()
        };
        var fatherOrder = new Order
        {
            Id = 1, TableId = 5, Status = "Active", FatherId = null,
            Table = new Table { Id = 5, Name = "Bàn 5", AreaId = 1 },
            OrderDetails = new List<OrderDetail>()
        };

        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _orderRepoMock.Setup(r => r.GetActiveOrderByTableIdAsync(10)).ReturnsAsync(childOrder);
        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(fatherOrder);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order> { childOrder });

        var result = await _service.GetTableOrderSummaryAsync(10);

        Assert.True(result.IsMerged);
        Assert.Equal(1, result.ActiveOrderId); // root father
    }
}
