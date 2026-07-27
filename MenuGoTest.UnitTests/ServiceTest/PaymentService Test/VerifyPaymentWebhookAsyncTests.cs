using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.PaymentServiceTest;

public class VerifyPaymentWebhookAsyncTests
{
    private readonly Mock<IPaymentRepository> _paymentRepoMock;
    private readonly Mock<IOrderService> _orderServiceMock;

    public VerifyPaymentWebhookAsyncTests()
    {
        _paymentRepoMock = new Mock<IPaymentRepository>();
        _orderServiceMock = new Mock<IOrderService>();
    }

    [Fact]
    public async Task UpdatesPaymentStatusToSuccess_WhenWebhookValid()
    {
        // Arrange
        var payment = new Payment
        {
            Id = 1,
            OrderId = 100,
            TransactionId = "12345678",
            Status = "Pending"
        };

        _paymentRepoMock.Setup(r => r.GetByTransactionIdPendingAsync("12345678")).ReturnsAsync(payment);
        _paymentRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);
        _orderServiceMock.Setup(s => s.PayOrderAsync(100, It.IsAny<OrderPayDto>(), "PayOS")).ReturnsAsync(new List<long> { 1, 2 });

        // Act
        payment.Status = "Success";
        payment.CompletedAt = DateTime.UtcNow;
        await _paymentRepoMock.Object.UpdateAsync(payment);
        var paidTableIds = await _orderServiceMock.Object.PayOrderAsync(payment.OrderId, new OrderPayDto(), "PayOS");

        // Assert
        Assert.Equal("Success", payment.Status);
        Assert.Equal(2, paidTableIds.Count);
        _paymentRepoMock.Verify(r => r.UpdateAsync(It.Is<Payment>(p => p.Status == "Success")), Times.Once);
        _orderServiceMock.Verify(s => s.PayOrderAsync(100, It.IsAny<OrderPayDto>(), "PayOS"), Times.Once);
    }

    [Fact]
    public async Task ReturnsFalse_WhenPaymentTransactionNotFound()
    {
        // Arrange
        _paymentRepoMock.Setup(r => r.GetByTransactionIdPendingAsync("non-existent")).ReturnsAsync((Payment?)null);

        // Act
        var result = await _paymentRepoMock.Object.GetByTransactionIdPendingAsync("non-existent");

        // Assert
        Assert.Null(result);
    }
}
