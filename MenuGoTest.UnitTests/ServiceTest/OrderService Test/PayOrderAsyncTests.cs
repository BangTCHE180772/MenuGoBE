using MenuGoBE.Dtos.Order;
using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderServiceTest;

public class PayOrderAsyncTests : OrderServiceTestBase
{
    private Order CreateActiveOrder(long id = 1, long tableId = 10, decimal totalAmount = 200000)
    {
        return new Order
        {
            Id = id, TableId = tableId, Status = "Active", TotalAmount = totalAmount,
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { Id = 1, OrderId = id, Quantity = 2, Price = 100000, Status = "Confirmed" }
            }
        };
    }

    [Fact]
    public async Task ReturnsTableIds_WhenPaymentSuccess()
    {
        var order = CreateActiveOrder();
        var table = new Table { Id = 10, Name = "Bàn 10", Status = "Occupied", AreaId = 1 };

        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order>());
        _customerRepoMock.Setup(r => r.GetByPhoneAsync(It.IsAny<string>())).ReturnsAsync((Customer?)null);
        _voucherRepoMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((Voucher?)null);
        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _paymentRepoMock.Setup(r => r.HasSuccessPaymentAsync(1)).ReturnsAsync(false);
        _paymentRepoMock.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.PayOrderAsync(1);

        Assert.Contains(10, result);
    }

    [Fact]
    public async Task SetsOrderStatusToPaid()
    {
        var order = CreateActiveOrder();
        var table = new Table { Id = 10, Status = "Occupied", AreaId = 1 };

        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order>());
        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _paymentRepoMock.Setup(r => r.HasSuccessPaymentAsync(1)).ReturnsAsync(false);
        _paymentRepoMock.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _service.PayOrderAsync(1);

        Assert.Equal("Paid", order.Status);
    }

    [Fact]
    public async Task SetsTableStatusToCleaning()
    {
        var order = CreateActiveOrder();
        var table = new Table { Id = 10, Status = "Occupied", AreaId = 1 };

        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order>());
        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _paymentRepoMock.Setup(r => r.HasSuccessPaymentAsync(1)).ReturnsAsync(false);
        _paymentRepoMock.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _service.PayOrderAsync(1);

        Assert.Equal("Cleaning", table.Status);
    }

    [Fact]
    public async Task CreatesPaymentRecord_ForCash()
    {
        var order = CreateActiveOrder();
        var table = new Table { Id = 10, Status = "Occupied", AreaId = 1 };

        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order>());
        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _paymentRepoMock.Setup(r => r.HasSuccessPaymentAsync(1)).ReturnsAsync(false);
        _paymentRepoMock.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _service.PayOrderAsync(1, paymentMethod: "Cash");

        _paymentRepoMock.Verify(r => r.CreateAsync(It.Is<Payment>(p => p.Method == "Cash" && p.Status == "Success")), Times.Once);
    }

    [Fact]
    public async Task DoesNotCreateDuplicatePayment_ForPayOS()
    {
        var order = CreateActiveOrder();
        var table = new Table { Id = 10, Status = "Occupied", AreaId = 1 };

        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order>());
        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _paymentRepoMock.Setup(r => r.HasSuccessPaymentAsync(1)).ReturnsAsync(true); // already has payment
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _service.PayOrderAsync(1, paymentMethod: "PayOS");

        _paymentRepoMock.Verify(r => r.CreateAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task PaysAllChildOrders()
    {
        var order = CreateActiveOrder();
        var childOrder = new Order { Id = 2, TableId = 20, Status = "Active", FatherId = 1, OrderDetails = new List<OrderDetail>() };
        var table = new Table { Id = 10, Status = "Occupied", AreaId = 1 };
        var childTable = new Table { Id = 20, Status = "Occupied", AreaId = 1 };

        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order> { childOrder });
        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _tableRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(childTable);
        _paymentRepoMock.Setup(r => r.HasSuccessPaymentAsync(1)).ReturnsAsync(false);
        _paymentRepoMock.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.PayOrderAsync(1);

        Assert.Equal("Paid", childOrder.Status);
        Assert.Equal("Cleaning", childTable.Status);
        Assert.Contains(20, result);
    }

    [Fact]
    public async Task ReturnsEmpty_WhenOrderNotFound()
    {
        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(999)).ReturnsAsync((Order?)null);

        var result = await _service.PayOrderAsync(999);

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdatesVoucherUsedCount()
    {
        var voucher = new Voucher { Id = 5, Code = "SALE", UsedCount = 3, Quantity = 100, IsActive = true, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(30), DiscountType = "Fixed", DiscountValue = 10000 };
        var order = CreateActiveOrder();
        order.VoucherId = 5;
        var table = new Table { Id = 10, Status = "Occupied", AreaId = 1 };

        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order>());
        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(table);
        _voucherRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(voucher);
        _voucherRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Voucher>())).ReturnsAsync(true);
        _paymentRepoMock.Setup(r => r.HasSuccessPaymentAsync(1)).ReturnsAsync(false);
        _paymentRepoMock.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _service.PayOrderAsync(1, paymentMethod: "PayOS");

        Assert.Equal(4, voucher.UsedCount);
    }
}
