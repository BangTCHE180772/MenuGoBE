using AutoMapper;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderDetailServiceTest;

public class GetByOrderIdAsyncTests
{
    private readonly Mock<IOrderDetailRepository> _detailRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly IMapper _mapper;
    private readonly OrderDetailService _service;

    public GetByOrderIdAsyncTests()
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
    public async Task ReturnsListOfDtos_WhenOrderHasItems()
    {
        // Arrange
        var details = new List<OrderDetail>
        {
            new OrderDetail { Id = 1, OrderId = 10, ProductId = 1, Quantity = 2, Price = 50000, Status = "Confirmed", CookingStatus = "Waiting", Product = new Product { Name = "Phở" } },
            new OrderDetail { Id = 2, OrderId = 10, ProductId = 2, Quantity = 1, Price = 30000, Status = "Confirmed", CookingStatus = "Cooking", Product = new Product { Name = "Trà đá" } }
        };
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(details);

        // Act
        var result = await _service.GetByOrderIdAsync(10);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenNoItems()
    {
        // Arrange
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(999)).ReturnsAsync(new List<OrderDetail>());

        // Act
        var result = await _service.GetByOrderIdAsync(999);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}

