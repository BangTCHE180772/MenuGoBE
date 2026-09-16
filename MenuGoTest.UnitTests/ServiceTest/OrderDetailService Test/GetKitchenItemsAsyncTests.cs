using AutoMapper;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderDetailServiceTest;

public class GetKitchenItemsAsyncTests
{
    private readonly Mock<IOrderDetailRepository> _detailRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly IMapper _mapper;
    private readonly OrderDetailService _service;

    public GetKitchenItemsAsyncTests()
    {
        _detailRepoMock = new Mock<IOrderDetailRepository>();
        _orderRepoMock = new Mock<IOrderRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new OrderDetailService(_detailRepoMock.Object, _orderRepoMock.Object, _mapper, new Mock<MenuGoBE.Interface.Repository.ITableRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IProductRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IAreaRepository>().Object, new Mock<MenuGoBE.Interface.Services.IKitchenService>().Object, new Mock<MenuGoBE.Interface.Repository.IBInventoryRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IRecipesDetailedRepository>().Object, new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MenuGoBE.Hubs.NotificationHub>>().Object, new Mock<MenuGoBE.Interface.Services.Document.IDocumentService>().Object, new Mock<MenuGoBE.Interface.Services.ILeftoverRecordService>().Object, new Mock<MenuGoBE.Interface.Services.IPromotionService>().Object);
    }

    [Fact]
    public async Task ReturnsOrderedList_ByCreatedAt()
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order
            {
                Id = 1, TableId = 10, Status = "Active",
                Table = new Table { Id = 10, Name = "Bàn 1", AreaId = 1, Area = new Area { Id = 1, Name = "T1", BranchId = 1 } },
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail { Id = 1, OrderId = 1, ProductId = 1, Quantity = 1, Price = 50000, Status = "Confirmed", CookingStatus = "Waiting", CreatedAt = DateTime.UtcNow.AddMinutes(-10), Product = new Product { Name = "Phở", Type = MenuGoBE.Models.Enums.ProductType.Processed } },
                    new OrderDetail { Id = 2, OrderId = 1, ProductId = 2, Quantity = 2, Price = 30000, Status = "Confirmed", CookingStatus = "Cooking", CreatedAt = DateTime.UtcNow.AddMinutes(-5), Product = new Product { Name = "Trà đá", Type = MenuGoBE.Models.Enums.ProductType.Processed } }
                }
            }
        };
        _orderRepoMock.Setup(r => r.GetActiveKitchenOrdersWithDetailsAsync()).ReturnsAsync(orders);

        // Act
        var result = await _service.GetKitchenItemsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result[0].CreatedAt <= result[1].CreatedAt); // sorted by CreatedAt ASC
    }

    [Fact]
    public async Task ExcludesCancelledItems()
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order
            {
                Id = 1, TableId = 10, Status = "Active",
                Table = new Table { Id = 10, Name = "Bàn 1", AreaId = 1, Area = new Area { Id = 1, Name = "T1", BranchId = 1 } },
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail { Id = 1, OrderId = 1, Quantity = 1, Price = 50000, Status = "Confirmed", CookingStatus = "Waiting", CreatedAt = DateTime.UtcNow, Product = new Product { Name = "Phở", Type = MenuGoBE.Models.Enums.ProductType.Processed } },
                    new OrderDetail { Id = 2, OrderId = 1, Quantity = 2, Price = 30000, Status = "Cancelled", CookingStatus = "Waiting", CreatedAt = DateTime.UtcNow, Product = new Product { Name = "Cancelled Item" } }
                }
            }
        };
        _orderRepoMock.Setup(r => r.GetActiveKitchenOrdersWithDetailsAsync()).ReturnsAsync(orders);

        // Act
        var result = await _service.GetKitchenItemsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Phở", result[0].ProductName);
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenNoActiveOrders()
    {
        // Arrange
        _orderRepoMock.Setup(r => r.GetActiveKitchenOrdersWithDetailsAsync()).ReturnsAsync(new List<Order>());

        // Act
        var result = await _service.GetKitchenItemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task MapsDefaultCookingStatus_WhenEmpty()
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order
            {
                Id = 1, TableId = 10, Status = "Active",
                Table = new Table { Id = 10, Name = "Bàn 1", AreaId = 1, Area = new Area { Id = 1, Name = "T1", BranchId = 1 } },
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail { Id = 1, OrderId = 1, Quantity = 1, Price = 50000, Status = "Confirmed", CookingStatus = "", CreatedAt = DateTime.UtcNow, Product = new Product { Name = "Phở", Type = MenuGoBE.Models.Enums.ProductType.Processed } }
                }
            }
        };
        _orderRepoMock.Setup(r => r.GetActiveKitchenOrdersWithDetailsAsync()).ReturnsAsync(orders);

        // Act
        var result = await _service.GetKitchenItemsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Waiting", result[0].CookingStatus);
    }
}

