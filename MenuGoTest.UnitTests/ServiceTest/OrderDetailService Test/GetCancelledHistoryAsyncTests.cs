using AutoMapper;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderDetailServiceTest;

public class GetCancelledHistoryAsyncTests
{
    private readonly Mock<IOrderDetailRepository> _detailRepoMock;
    private readonly OrderDetailService _service;

    public GetCancelledHistoryAsyncTests()
    {
        _detailRepoMock = new Mock<IOrderDetailRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        var mapper = config.CreateMapper();
        _service = new OrderDetailService(
            _detailRepoMock.Object,
            new Mock<IOrderRepository>().Object,
            mapper,
            new Mock<ITableRepository>().Object,
            new Mock<IProductRepository>().Object,
            new Mock<IAreaRepository>().Object,
            new Mock<IKitchenService>().Object,
            new Mock<IBInventoryRepository>().Object,
            new Mock<IRecipesDetailedRepository>().Object,
            new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MenuGoBE.Hubs.NotificationHub>>().Object,
            new Mock<IDocumentService>().Object,
            new Mock<ILeftoverRecordService>().Object);
    }

    [Fact]
    public async Task CategorizesAsCompleted_WhenCancelledFromReady()
    {
        var order = new Order { Id = 1, Table = new Table { Id = 1, Name = "Bàn 3" } };
        var details = new List<OrderDetail>
        {
            new OrderDetail { Id = 1, OrderId = 1, ProductId = 5, Quantity = 2, Price = 40000, Status = "Cancelled", CookingStatus = "Ready", Order = order, Product = new Product { Id = 5, Name = "Phở bò" } },
        };
        _detailRepoMock.Setup(r => r.GetCancelledByBranchAsync(3, null, null)).ReturnsAsync(details);

        var result = await _service.GetCancelledHistoryAsync(3, null, null);

        Assert.Single(result);
        Assert.Equal("Completed", result[0].Category);
        Assert.Equal("Bàn 3", result[0].TableName);
        Assert.Equal("Phở bò", result[0].ProductName);
    }

    [Fact]
    public async Task CategorizesAsPending_WhenCancelledFromWaiting()
    {
        var order = new Order { Id = 1, Table = new Table { Id = 1, Name = "Bàn 4" } };
        var details = new List<OrderDetail>
        {
            new OrderDetail { Id = 2, OrderId = 1, ProductId = 6, Quantity = 1, Price = 20000, Status = "Cancelled", CookingStatus = "Waiting", Order = order, Product = new Product { Id = 6, Name = "Trà đá" } },
        };
        _detailRepoMock.Setup(r => r.GetCancelledByBranchAsync(3, null, null)).ReturnsAsync(details);

        var result = await _service.GetCancelledHistoryAsync(3, null, null);

        Assert.Single(result);
        Assert.Equal("Pending", result[0].Category);
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenNoCancelledItems()
    {
        _detailRepoMock.Setup(r => r.GetCancelledByBranchAsync(3, null, null)).ReturnsAsync(new List<OrderDetail>());

        var result = await _service.GetCancelledHistoryAsync(3, null, null);

        Assert.Empty(result);
    }
}

