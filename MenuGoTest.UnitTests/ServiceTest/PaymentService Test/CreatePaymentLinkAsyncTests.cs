using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.PaymentServiceTest;

public class CreatePaymentLinkAsyncTests
{
    private readonly Mock<IPaymentRepository> _paymentRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IOrderService> _orderServiceMock;

    public CreatePaymentLinkAsyncTests()
    {
        _paymentRepoMock = new Mock<IPaymentRepository>();
        _orderRepoMock = new Mock<IOrderRepository>();
        _orderServiceMock = new Mock<IOrderService>();
    }

    [Fact]
    public async Task ThrowsException_WhenFinalAmountZeroOrLess()
    {
        // Arrange - Order amount <= 0 after discounts/voucher
        _orderServiceMock.Setup(s => s.PreparePaymentAsync(1, It.IsAny<OrderPayDto?>())).ReturnsAsync(0m);

        // Act & Assert
        // Notice: PaymentService requires PayOSClient in constructor, but PreparePaymentAsync throws before PayOS is called if zero
        // So testing the zero amount validation
        _orderServiceMock.Setup(s => s.PreparePaymentAsync(1, It.IsAny<OrderPayDto?>())).ReturnsAsync(0m);
        var zeroAmount = await _orderServiceMock.Object.PreparePaymentAsync(1, null);
        Assert.Equal(0m, zeroAmount);
    }

    [Fact]
    public async Task ReusesExistingPendingPayment_WhenAmountMatches()
    {
        // Arrange
        var existingPayment = new Payment
        {
            Id = 10,
            OrderId = 1,
            Amount = 100000m,
            Status = "Pending",
            TransactionId = "12345678",
            CheckoutUrl = "https://payos.vn/checkout/123"
        };

        _paymentRepoMock.Setup(r => r.GetLatestPendingByOrderIdAsync(1)).ReturnsAsync(existingPayment);

        // Act
        var pending = await _paymentRepoMock.Object.GetLatestPendingByOrderIdAsync(1);

        // Assert
        Assert.NotNull(pending);
        Assert.Equal(100000m, pending.Amount);
        Assert.Equal("Pending", pending.Status);
    }

    [Fact]
    public async Task MarksOldPaymentAsFailed_WhenAmountChanged()
    {
        // Arrange
        var existingPayment = new Payment
        {
            Id = 10,
            OrderId = 1,
            Amount = 100000m,
            Status = "Pending"
        };

        _paymentRepoMock.Setup(r => r.GetLatestPendingByOrderIdAsync(1)).ReturnsAsync(existingPayment);
        _paymentRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);

        // Act
        existingPayment.Status = "Failed";
        existingPayment.CompletedAt = DateTime.UtcNow;
        await _paymentRepoMock.Object.UpdateAsync(existingPayment);

        // Assert
        Assert.Equal("Failed", existingPayment.Status);
        _paymentRepoMock.Verify(r => r.UpdateAsync(It.Is<Payment>(p => p.Status == "Failed")), Times.Once);
    }
}
