using AutoMapper;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderDetailServiceTest;

public class UpdateCookingStatusAsyncTests
{
    private readonly Mock<IOrderDetailRepository> _detailRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly IMapper _mapper;
    private readonly OrderDetailService _service;

    public UpdateCookingStatusAsyncTests()
    {
        _detailRepoMock = new Mock<IOrderDetailRepository>();
        _orderRepoMock = new Mock<IOrderRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new OrderDetailService(_detailRepoMock.Object, _orderRepoMock.Object, _mapper, new Mock<MenuGoBE.Interface.Repository.ITableRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IProductRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IAreaRepository>().Object, new Mock<MenuGoBE.Interface.Services.IKitchenService>().Object, new Mock<MenuGoBE.Interface.Repository.IBInventoryRepository>().Object, new Mock<MenuGoBE.Interface.Repository.IRecipesDetailedRepository>().Object, new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MenuGoBE.Hubs.NotificationHub>>().Object, new Mock<MenuGoBE.Interface.Services.Document.IDocumentService>().Object);
    }

    [Fact]
    public async Task ReturnsTrue_WhenValidStatus()
    {
        // Arrange
        _detailRepoMock.Setup(r => r.UpdateCookingStatusAsync(1, "Cooking")).ReturnsAsync(true);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateCookingStatusAsync(1, "Cooking");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenUpdateFails()
    {
        // Arrange
        _detailRepoMock.Setup(r => r.UpdateCookingStatusAsync(999, "Cooking")).ReturnsAsync(false);

        // Act
        var result = await _service.UpdateCookingStatusAsync(999, "Cooking");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsTrue_WhenStatusIsReady()
    {
        // Arrange
        _detailRepoMock.Setup(r => r.UpdateCookingStatusAsync(1, "Ready")).ReturnsAsync(true);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateCookingStatusAsync(1, "Ready");

        // Assert
        Assert.True(result);
    }
}
