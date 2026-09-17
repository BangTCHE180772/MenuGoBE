using MenuGoBE.Dtos.Leftover;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.LeftoverRecordServiceTest;

public class LeftoverRecordServiceTests
{
    private readonly Mock<ILeftoverRecordRepository> _repoMock;
    private readonly LeftoverRecordService _service;

    public LeftoverRecordServiceTests()
    {
        _repoMock = new Mock<ILeftoverRecordRepository>();
        _service = new LeftoverRecordService(_repoMock.Object);

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<LeftoverRecord>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((LeftoverRecord?)null);
    }

    #region CreateAsync - shared validation

    [Fact]
    public async Task CreateAsync_Throws_WhenQuantityIsZero()
    {
        var dto = new LeftoverRecordCreateDto { BranchId = 1, Type = LeftoverType.Extra, ProductId = 1, Quantity = 0, Reason = "abc" };

        await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(dto, 1));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenQuantityIsNegative()
    {
        var dto = new LeftoverRecordCreateDto { BranchId = 1, Type = LeftoverType.Extra, ProductId = 1, Quantity = -2, Reason = "abc" };

        await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(dto, 1));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenReasonEmpty()
    {
        var dto = new LeftoverRecordCreateDto { BranchId = 1, Type = LeftoverType.Extra, ProductId = 1, Quantity = 1, Reason = "   " };

        await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(dto, 1));
    }

    #endregion

    #region CreateAsync - Return (Khách trả món)

    [Fact]
    public async Task CreateAsync_Return_Throws_WhenOrderDetailIdMissing()
    {
        var dto = new LeftoverRecordCreateDto
        {
            BranchId = 1,
            Type = LeftoverType.Return,
            ProductId = 1,
            Quantity = 1,
            Reason = "Nguội",
            HandlingAction = LeftoverHandlingAction.Discard,
        };

        await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(dto, 1));
    }

    [Fact]
    public async Task CreateAsync_Return_Throws_WhenHandlingActionMissing()
    {
        var dto = new LeftoverRecordCreateDto
        {
            BranchId = 1,
            Type = LeftoverType.Return,
            ProductId = 1,
            Quantity = 1,
            Reason = "Nguội",
            OrderDetailId = 100,
        };

        await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(dto, 1));
    }

    [Fact]
    public async Task CreateAsync_Return_Throws_WhenOrderDetailNotFound()
    {
        var dto = new LeftoverRecordCreateDto
        {
            BranchId = 1,
            Type = LeftoverType.Return,
            ProductId = 1,
            Quantity = 1,
            Reason = "Nguội",
            OrderDetailId = 999,
            HandlingAction = LeftoverHandlingAction.Discard,
        };
        _repoMock.Setup(r => r.GetOrderDetailWithTableAsync(999)).ReturnsAsync((OrderDetail?)null);

        await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(dto, 1));
    }

    [Fact]
    public async Task CreateAsync_Return_Succeeds_WhenValid()
    {
        var order = new Order { Id = 10, TableId = 5, Table = new Table { Id = 5, Name = "Bàn 5" } };
        var orderDetail = new OrderDetail { Id = 100, OrderId = 10, ProductId = 1, Order = order };
        _repoMock.Setup(r => r.GetOrderDetailWithTableAsync(100)).ReturnsAsync(orderDetail);

        var dto = new LeftoverRecordCreateDto
        {
            BranchId = 1,
            Type = LeftoverType.Return,
            ProductId = 1,
            Quantity = 2,
            Reason = "Khách gọi nhầm",
            OrderDetailId = 100,
            HandlingAction = LeftoverHandlingAction.Reuse,
        };

        var result = await _service.CreateAsync(dto, 42);

        Assert.Equal(LeftoverType.Return, result.Type);
        Assert.Equal("Bàn 5", result.TableName);
        Assert.Equal(LeftoverHandlingAction.Reuse, result.HandlingAction);
        _repoMock.Verify(r => r.CreateAsync(It.Is<LeftoverRecord>(l =>
            l.Type == LeftoverType.Return &&
            l.OrderDetailId == 100 &&
            l.CreatedBy == 42)), Times.Once);
    }

    #endregion

    #region CreateAsync - Extra (Bếp làm dư)

    [Fact]
    public async Task CreateAsync_Extra_Throws_WhenOrderDetailIdProvided()
    {
        var dto = new LeftoverRecordCreateDto
        {
            BranchId = 1,
            Type = LeftoverType.Extra,
            ProductId = 1,
            Quantity = 1,
            Reason = "Làm dư",
            OrderDetailId = 100,
        };

        await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(dto, 1));
    }

    [Fact]
    public async Task CreateAsync_Extra_Throws_WhenAtFaultAccountProvided()
    {
        var dto = new LeftoverRecordCreateDto
        {
            BranchId = 1,
            Type = LeftoverType.Extra,
            ProductId = 1,
            Quantity = 1,
            Reason = "Làm dư",
            AtFaultAccountId = 9,
        };

        await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(dto, 1));
    }

    [Fact]
    public async Task CreateAsync_Extra_Succeeds_WhenValid_AndNeverTouchesOrderOrStock()
    {
        var dto = new LeftoverRecordCreateDto
        {
            BranchId = 1,
            Type = LeftoverType.Extra,
            ProductId = 3,
            Quantity = 5,
            Reason = "Estimate sai",
            ShiftId = 2,
        };

        var result = await _service.CreateAsync(dto, 7);

        Assert.Equal(LeftoverType.Extra, result.Type);
        Assert.Null(result.OrderDetailId);
        Assert.Null(result.AtFaultAccountId);
        // The repo mock has no order/inventory repositories at all — if the service
        // tried to touch Order.TotalAmount or BInventory it would have nothing to call.
        _repoMock.Verify(r => r.GetOrderDetailWithTableAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Extra_DefaultsRecordDateToToday_WhenNotProvided()
    {
        var dto = new LeftoverRecordCreateDto
        {
            BranchId = 1,
            Type = LeftoverType.Extra,
            ProductId = 3,
            Quantity = 1,
            Reason = "Làm dư",
        };

        var result = await _service.CreateAsync(dto, null);

        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), result.RecordDate);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((LeftoverRecord?)null);

        var result = await _service.DeleteAsync(1);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new LeftoverRecord { Id = 1, BranchId = 1, ProductId = 1, Quantity = 1, Reason = "x" });
        _repoMock.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1);

        Assert.True(result);
        _repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    #endregion

    #region GetByBranchAsync

    [Fact]
    public async Task GetByBranchAsync_MapsProductAndTableNames()
    {
        var order = new Order { Id = 1, Table = new Table { Id = 1, Name = "Bàn 9" } };
        var orderDetail = new OrderDetail { Id = 5, Order = order };
        var records = new List<LeftoverRecord>
        {
            new LeftoverRecord
            {
                Id = 1, BranchId = 1, Type = LeftoverType.Return, ProductId = 1,
                Product = new Product { Id = 1, Name = "Phở bò" },
                Quantity = 1, Reason = "Nguội", OrderDetail = orderDetail, OrderDetailId = 5,
            },
        };
        _repoMock.Setup(r => r.GetByBranchAsync(1, null, null, null)).ReturnsAsync(records);

        var result = await _service.GetByBranchAsync(1, null, null, null);

        Assert.Single(result);
        Assert.Equal("Phở bò", result[0].ProductName);
        Assert.Equal("Bàn 9", result[0].TableName);
    }

    #endregion
}
