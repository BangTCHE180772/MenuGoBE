using MenuGoBE.Dtos.Order;
using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.OrderServiceTest;

public class PreparePaymentAsyncTests : OrderServiceTestBase
{
    private Order CreateOrderWithDetails(decimal price = 100000, int qty = 2)
    {
        return new Order
        {
            Id = 1, TableId = 10, Status = "Active", TotalAmount = 0,
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { Id = 1, OrderId = 1, Quantity = qty, Price = price, Status = "Confirmed" }
            }
        };
    }

    [Fact]
    public async Task ThrowsException_WhenOrderNotFound()
    {
        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(999)).ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.PreparePaymentAsync(999, null));
    }

    [Fact]
    public async Task CalculatesTotalAmount_IncludingVAT8Percent()
    {
        // 2 x 100000 = 200000 + 8% VAT = 216000
        var order = CreateOrderWithDetails(100000, 2);
        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order>());
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.PreparePaymentAsync(1, null);

        Assert.Equal(216000, result);
    }

    [Fact]
    public async Task CalculatesTotal_IncludesChildOrders()
    {
        // Father: 2 x 100k = 200k, Child: 1 x 50k = 50k → Total: 250k + 8% = 270k
        var father = CreateOrderWithDetails(100000, 2);
        var child = new Order
        {
            Id = 2, TableId = 20, Status = "Active", FatherId = 1,
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { Id = 2, OrderId = 2, Quantity = 1, Price = 50000, Status = "Confirmed" }
            }
        };

        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(father);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order> { child });
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.PreparePaymentAsync(1, null);

        Assert.Equal(270000, result);
    }

    [Fact]
    public async Task AppliesFixedVoucher_Correctly()
    {
        // Total: 200k + 8% = 216k, Voucher: -50k → 166k
        var order = CreateOrderWithDetails(100000, 2);
        var voucher = new Voucher
        {
            Id = 1, Code = "FIX50", DiscountType = "Fixed", DiscountValue = 50000,
            MinOrderValue = 100000, Quantity = 100, UsedCount = 0, IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(30)
        };

        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order>());
        _voucherRepoMock.Setup(r => r.GetByCodeAsync("FIX50")).ReturnsAsync(voucher);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = new OrderPayDto { VoucherCode = "FIX50" };
        var result = await _service.PreparePaymentAsync(1, dto);

        Assert.Equal(166000, result);
    }

    [Fact]
    public async Task AppliesPercentageVoucher_WithMaxCap()
    {
        // Total: 200k + 8% = 216k, Voucher 50% → 108k, but MaxDiscount = 30k → -30k → 186k
        var order = CreateOrderWithDetails(100000, 2);
        var voucher = new Voucher
        {
            Id = 1, Code = "PCT50", DiscountType = "Percentage", DiscountValue = 50, MaxDiscount = 30000,
            MinOrderValue = 100000, Quantity = 100, UsedCount = 0, IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(30)
        };

        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order>());
        _voucherRepoMock.Setup(r => r.GetByCodeAsync("PCT50")).ReturnsAsync(voucher);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = new OrderPayDto { VoucherCode = "PCT50" };
        var result = await _service.PreparePaymentAsync(1, dto);

        Assert.Equal(186000, result);
    }

    [Fact]
    public async Task CreatesNewCustomer_WhenPhoneNotExists()
    {
        var order = CreateOrderWithDetails(100000, 2);

        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order>());
        _customerRepoMock.Setup(r => r.GetByPhoneAsync("0987654321")).ReturnsAsync((Customer?)null);
        _customerRepoMock.Setup(r => r.CreateAsync(It.IsAny<Customer>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = new OrderPayDto { CustomerPhone = "0987654321", CustomerName = "Nguyễn Văn A" };
        await _service.PreparePaymentAsync(1, dto);

        _customerRepoMock.Verify(r => r.CreateAsync(It.Is<Customer>(c => c.Phone == "0987654321" && c.Name == "Nguyễn Văn A")), Times.Once);
    }

    [Fact]
    public async Task DeductsPoints_WhenCustomerHasEnough()
    {
        var order = CreateOrderWithDetails(100000, 2);
        order.CustomerId = 1;
        // Point >= PointSystemConfig.MinPointsToUse (50000 mặc định) để đủ điều kiện dùng điểm
        var customer = new Customer { Id = 1, Name = "Test", Phone = "0987654321", Point = 50200 };

        _orderRepoMock.Setup(r => r.GetActiveOrderByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(1)).ReturnsAsync(new List<Order>());
        _customerRepoMock.Setup(r => r.GetByPhoneAsync("0987654321")).ReturnsAsync(customer);
        _customerRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = new OrderPayDto { CustomerPhone = "0987654321", PointsUsed = 200 };
        var result = await _service.PreparePaymentAsync(1, dto, paymentMethod: "PayOS");

        // 216000 - 200 points = 215800
        Assert.Equal(215800, result);
    }
}
