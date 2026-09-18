using AutoMapper;
using MenuGoBE.Dtos.Leftover;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderDetailServiceTest;

public class CancelOrderDetailAsyncTests
{
    private readonly Mock<IOrderDetailRepository> _detailRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<ITableRepository> _tableRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<IAreaRepository> _areaRepoMock;
    private readonly Mock<IBInventoryRepository> _bInventoryRepoMock;
    private readonly Mock<ILeftoverRecordService> _leftoverServiceMock;
    private readonly IMapper _mapper;
    private readonly OrderDetailService _service;

    public CancelOrderDetailAsyncTests()
    {
        _detailRepoMock = new Mock<IOrderDetailRepository>();
        _orderRepoMock = new Mock<IOrderRepository>();
        _tableRepoMock = new Mock<ITableRepository>();
        _productRepoMock = new Mock<IProductRepository>();
        _areaRepoMock = new Mock<IAreaRepository>();
        _bInventoryRepoMock = new Mock<IBInventoryRepository>();
        _leftoverServiceMock = new Mock<ILeftoverRecordService>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new OrderDetailService(
            _detailRepoMock.Object,
            _orderRepoMock.Object,
            _mapper,
            _tableRepoMock.Object,
            _productRepoMock.Object,
            _areaRepoMock.Object,
            new Mock<IKitchenService>().Object,
            _bInventoryRepoMock.Object,
            new Mock<IRecipesDetailedRepository>().Object,
            new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MenuGoBE.Hubs.NotificationHub>>().Object,
            new Mock<IDocumentService>().Object,
            _leftoverServiceMock.Object);
    }

    [Fact]
    public async Task ReturnsTrue_WhenWaitingStatus()
    {
        // Arrange
        var detail = new OrderDetail { Id = 1, OrderId = 10, Quantity = 2, Price = 50000, Status = "Confirmed", CookingStatus = "Waiting" };
        var order = new Order { Id = 10, TableId = 1, Status = "Active", TotalAmount = 100000 };

        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(detail);
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(1, "Cancelled")).ReturnsAsync(true);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(new List<OrderDetail>
        {
            new OrderDetail { Id = 1, OrderId = 10, Quantity = 2, Price = 50000, Status = "Cancelled" }
        });
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CancelOrderDetailAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenNotExists()
    {
        // Arrange
        _detailRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((OrderDetail?)null);

        // Act
        var result = await _service.CancelOrderDetailAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenAlreadyCancelled()
    {
        // Arrange
        var detail = new OrderDetail { Id = 1, OrderId = 10, Status = "Cancelled", CookingStatus = "Waiting" };
        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(detail);

        // Act
        var result = await _service.CancelOrderDetailAsync(1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsFalse_WhenCookingInProgress()
    {
        // Arrange - CookingStatus = "Cooking" → không cho hủy (chỉ Waiting/Ready mới được)
        var detail = new OrderDetail { Id = 1, OrderId = 10, Status = "Confirmed", CookingStatus = "Cooking" };
        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(detail);

        // Act
        var result = await _service.CancelOrderDetailAsync(1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsTrue_WhenReadyStatus_KitchenWasLateFlow()
    {
        // Arrange - CookingStatus = "Ready" nay ĐƯỢC phép huỷ (luồng "bếp làm chậm, khách không nhận")
        var detail = new OrderDetail { Id = 1, OrderId = 10, ProductId = 5, Quantity = 2, Status = "Confirmed", CookingStatus = "Ready" };
        var order = new Order { Id = 10, TableId = 1, Status = "Active", TotalAmount = 100000 };
        var table = new Table { Id = 1, AreaId = 2, Name = "Bàn 1" };
        var area = new Area { Id = 2, BranchId = 3, Name = "Khu A" };

        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(detail);
        _productRepoMock.Setup(r => r.GetProductTypeAsync(5)).ReturnsAsync("Processed");
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(1, "Cancelled")).ReturnsAsync(true);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(new List<OrderDetail>());
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _areaRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(area);

        // Act
        var result = await _service.CancelOrderDetailAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CreatesLeftoverExtraRecord_WhenCancellingReadyProcessedItem()
    {
        // Arrange
        var detail = new OrderDetail { Id = 1, OrderId = 10, ProductId = 5, Quantity = 3, Status = "Confirmed", CookingStatus = "Ready" };
        var order = new Order { Id = 10, TableId = 1, Status = "Active", TotalAmount = 100000 };
        var table = new Table { Id = 1, AreaId = 2, Name = "Bàn 1" };
        var area = new Area { Id = 2, BranchId = 7, Name = "Khu A" };

        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(detail);
        _productRepoMock.Setup(r => r.GetProductTypeAsync(5)).ReturnsAsync("Processed");
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(1, "Cancelled")).ReturnsAsync(true);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(new List<OrderDetail>());
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _areaRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(area);

        // Act
        await _service.CancelOrderDetailAsync(1);

        // Assert
        _leftoverServiceMock.Verify(s => s.CreateAsync(
            It.Is<LeftoverRecordCreateDto>(dto =>
                dto.Type == LeftoverType.Extra &&
                dto.BranchId == 7 &&
                dto.ProductId == 5 &&
                dto.Quantity == 3 &&
                dto.OrderDetailId == null),
            null), Times.Once);
    }

    [Fact]
    public async Task DoesNotRestockInventory_WhenCancellingReadyProcessedItem()
    {
        // Arrange - Processed: nguyên liệu đã dùng khi nấu, huỷ sau khi Ready không được cộng trả kho
        var detail = new OrderDetail { Id = 1, OrderId = 10, ProductId = 5, Quantity = 1, Status = "Confirmed", CookingStatus = "Ready" };
        var order = new Order { Id = 10, TableId = 1, Status = "Active", TotalAmount = 100000 };
        var table = new Table { Id = 1, AreaId = 2, Name = "Bàn 1" };
        var area = new Area { Id = 2, BranchId = 7, Name = "Khu A" };

        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(detail);
        _productRepoMock.Setup(r => r.GetProductTypeAsync(5)).ReturnsAsync("Processed");
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(1, "Cancelled")).ReturnsAsync(true);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(new List<OrderDetail>());
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _areaRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(area);

        // Act
        await _service.CancelOrderDetailAsync(1);

        // Assert
        _bInventoryRepoMock.Verify(r => r.GetByProductAndBranchAsync(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task RestocksInventory_WhenCancellingReadyRegularItem()
    {
        // Arrange - Regular: đã trừ kho lúc Confirm, huỷ (dù đang ở Ready) phải cộng lại kho
        var detail = new OrderDetail { Id = 1, OrderId = 10, ProductId = 9, Quantity = 2, Status = "Confirmed", CookingStatus = "Ready" };
        var order = new Order { Id = 10, TableId = 1, Status = "Active", TotalAmount = 100000 };
        var table = new Table { Id = 1, AreaId = 2, Name = "Bàn 1" };
        var area = new Area { Id = 2, BranchId = 7, Name = "Khu A" };
        var binv = new BInventory { Id = 1, ProductId = 9, BranchId = 7, Quantity = 10, IsManageQuantity = true };

        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(detail);
        _productRepoMock.Setup(r => r.GetProductTypeAsync(9)).ReturnsAsync("Regular");
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(1, "Cancelled")).ReturnsAsync(true);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(new List<OrderDetail>());
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _areaRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(area);
        _bInventoryRepoMock.Setup(r => r.GetByProductAndBranchAsync(9, 7)).ReturnsAsync(binv);
        _bInventoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<BInventory>())).Returns(Task.CompletedTask);
        _bInventoryRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.CancelOrderDetailAsync(1);

        // Assert
        Assert.Equal(12, binv.Quantity);
        _leftoverServiceMock.Verify(s => s.CreateAsync(It.IsAny<LeftoverRecordCreateDto>(), It.IsAny<long?>()), Times.Never);
    }

    [Fact]
    public async Task RecalculatesOrderTotal_AfterCancel()
    {
        // Arrange - hủy 1 món, còn 1 món 30000 x 1
        var detail = new OrderDetail { Id = 1, OrderId = 10, Quantity = 2, Price = 50000, Status = "Confirmed", CookingStatus = "Waiting" };
        var order = new Order { Id = 10, TableId = 1, Status = "Active", TotalAmount = 130000 };

        _detailRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(detail);
        _detailRepoMock.Setup(r => r.UpdateStatusAsync(1, "Cancelled")).ReturnsAsync(true);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _detailRepoMock.Setup(r => r.GetByOrderIdAsync(10)).ReturnsAsync(new List<OrderDetail>
        {
            new OrderDetail { Id = 1, OrderId = 10, Quantity = 2, Price = 50000, Status = "Cancelled" },
            new OrderDetail { Id = 2, OrderId = 10, Quantity = 1, Price = 30000, Status = "Confirmed" }
        });
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.CancelOrderDetailAsync(1);

        // Assert - only non-cancelled: 1 * 30000 = 30000
        _orderRepoMock.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.TotalAmount == 30000)), Times.Once);
    }
}

