using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.PaymentServiceTest;

public class CheckPaymentStatusAsyncTests
{
    private readonly Mock<IPaymentRepository> _paymentRepoMock;
    private readonly Mock<IOrderService> _orderServiceMock;

    public CheckPaymentStatusAsyncTests()
    {
        _paymentRepoMock = new Mock<IPaymentRepository>();
        _orderServiceMock = new Mock<IOrderService>();
    }

    [Fact]
    public async Task ThrowsException_WhenNoPendingPayment()
    {
        // Arrange
        _paymentRepoMock.Setup(r => r.GetLatestPendingByOrderIdAsync(1)).ReturnsAsync((Payment?)null);

        // Act
        var result = await _paymentRepoMock.Object.GetLatestPendingByOrderIdAsync(1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task HandlesTransactionIdEmpty_Correctly()
    {
        // Arrange
        var payment = new Payment { Id = 1, OrderId = 1, TransactionId = "", Status = "Pending" };
        _paymentRepoMock.Setup(r => r.GetLatestPendingByOrderIdAsync(1)).ReturnsAsync(payment);

        // Act
        var result = await _paymentRepoMock.Object.GetLatestPendingByOrderIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.True(string.IsNullOrEmpty(result.TransactionId));
    }

    [Fact]
    public async Task UpdatesPaymentToSuccess_WhenStatusIsPaid()
    {
        // Arrange
        var payment = new Payment { Id = 1, OrderId = 100, TransactionId = "123456", Status = "Pending" };
        _paymentRepoMock.Setup(r => r.GetLatestPendingByOrderIdAsync(100)).ReturnsAsync(payment);
        _paymentRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);
        _orderServiceMock.Setup(s => s.PayOrderAsync(100, It.IsAny<OrderPayDto?>(), "PayOS")).ReturnsAsync(new List<long> { 5 });

        // Act
        payment.Status = "Success";
        payment.CompletedAt = DateTime.UtcNow;
        await _paymentRepoMock.Object.UpdateAsync(payment);
        var paidTableIds = await _orderServiceMock.Object.PayOrderAsync(100, null, "PayOS");

        // Assert
        Assert.Equal("Success", payment.Status);
        Assert.Single(paidTableIds);
        Assert.Equal(5, paidTableIds[0]);
    }

    [Fact]
    public async Task UpdatesPaymentToFailed_WhenStatusIsCancelled()
    {
        // Arrange
        var payment = new Payment { Id = 1, OrderId = 100, TransactionId = "123456", Status = "Pending" };
        _paymentRepoMock.Setup(r => r.GetLatestPendingByOrderIdAsync(100)).ReturnsAsync(payment);
        _paymentRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);

        // Act
        payment.Status = "Failed";
        payment.CompletedAt = DateTime.UtcNow;
        await _paymentRepoMock.Object.UpdateAsync(payment);

        // Assert
        Assert.Equal("Failed", payment.Status);
        _paymentRepoMock.Verify(r => r.UpdateAsync(It.Is<Payment>(p => p.Status == "Failed")), Times.Once);
    }
}
