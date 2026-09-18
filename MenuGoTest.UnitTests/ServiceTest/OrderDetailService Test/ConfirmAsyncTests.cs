using AutoMapper;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderDetailServiceTest;

public class ConfirmAsyncTests
{
    private readonly Mock<IOrderDetailRepository> _detailRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly IMapper _mapper;
    private readonly OrderDetailService _service;

    public ConfirmAsyncTests()
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
    public async Task ReturnsTrue_WhenStatusUpdated()
    {
        // Arrange
        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new OrderDetail { Id = 1, OrderId = 1, ProductId = 1, Quantity = 1 });
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(1, "Confirmed")).ReturnsAsync(true);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.ConfirmAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenUpdateFails()
    {
        // Arrange
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(999, "Confirmed")).ReturnsAsync(false);

        // Act
        var result = await _service.ConfirmAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CallsSaveChanges_OnlyWhenSuccess()
    {
        // Arrange
        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new OrderDetail { Id = 1, OrderId = 1, ProductId = 1, Quantity = 1 });
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(1, "Confirmed")).ReturnsAsync(true);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.ConfirmAsync(1);

        // Assert
        _detailRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DoesNotCallSaveChanges_WhenFails()
    {
        // Arrange
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(999, "Confirmed")).ReturnsAsync(false);

        // Act
        await _service.ConfirmAsync(999);

        // Assert
        _detailRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}

