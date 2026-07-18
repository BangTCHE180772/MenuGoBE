using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models.Webhooks;
using PayOS.Models.V2.PaymentRequests;
using System.Text.Json;
using MenuGoBE.Dtos.Order;

namespace MenuGoBE.Service;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly PayOSClient _payOS;
    private readonly IOrderService _orderService;

    public PaymentService(IPaymentRepository paymentRepo, IOrderRepository orderRepo, PayOSClient payOS, IOrderService orderService)
    {
        _paymentRepo = paymentRepo;
        _orderRepo = orderRepo;
        _payOS = payOS;
        _orderService = orderService;
    }

    public async Task<Payment> CreatePaymentLinkAsync(long orderId, string cancelUrl, string returnUrl, OrderPayDto? dto = null)
    {
        // Calculate and prepare the payment on Order (apply voucher, points)
        decimal finalAmount = await _orderService.PreparePaymentAsync(orderId, dto);
        
        if (finalAmount <= 0) throw new Exception("Đơn hàng không có số tiền cần thanh toán qua PayOS (hoặc đã được miễn phí hoàn toàn).");

        // 2. Check if there's already a pending PayOS payment for this order
        var existingPayment = await _paymentRepo.GetLatestPendingByOrderIdAsync(orderId);

        if (existingPayment != null && existingPayment.Amount == finalAmount)
        {
            // Re-use existing QR Code instead of calling API again to avoid generating multiple payment links
            return existingPayment;
        }

        // If amount changed or no pending payment, cancel the old one (mark as Failed)
        if (existingPayment != null)
        {
            existingPayment.Status = "Failed";
            existingPayment.CompletedAt = DateTime.UtcNow;
            await _paymentRepo.UpdateAsync(existingPayment);
        }

        // 3. Create new PayOS link
        int orderCode = int.Parse(DateTime.Now.ToString("ddHHmmss")); // Unique 32-bit int
        
        var paymentData = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = (int)finalAmount,
            Description = $"Thanh toan don {orderId}",
            CancelUrl = cancelUrl,
            ReturnUrl = returnUrl
        };

        var createPaymentResult = await _payOS.PaymentRequests.CreateAsync(paymentData);

        // 4. Save to Database
        var payment = new Payment
        {
            OrderId = orderId,
            Amount = finalAmount,
            Method = "PayOS",
            Status = "Pending",
            TransactionId = orderCode.ToString(),
            PaymentLinkId = createPaymentResult.PaymentLinkId ?? string.Empty,
            CheckoutUrl = createPaymentResult.CheckoutUrl ?? string.Empty,
            QrCode = createPaymentResult.QrCode ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        await _paymentRepo.CreateAsync(payment);

        return payment;
    }

    public async Task<(bool verified, List<long> paidTableIds)> VerifyPaymentWebhookAsync(Webhook webhookBody)
    {
        try
        {
            // Verify signature
            var webhookData = await _payOS.Webhooks.VerifyAsync(webhookBody);

            if (webhookData.Code == "00")
            {
                // Find payment by TransactionId (orderCode)
                var transactionId = webhookData.OrderCode.ToString();
                var payment = await _paymentRepo.GetByTransactionIdPendingAsync(transactionId);

                if (payment != null)
                {
                    payment.Status = "Success";
                    payment.CompletedAt = DateTime.UtcNow;
                    await _paymentRepo.UpdateAsync(payment);

                    // Complete the Order automatically
                    var paidTableIds = await _orderService.PayOrderAsync(payment.OrderId, new OrderPayDto(), paymentMethod: "PayOS");
                    return (true, paidTableIds);
                }
            }
            return (false, new List<long>());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Webhook verify failed: {ex.Message}");
            return (false, new List<long>());
        }
    }

    public async Task<(Payment payment, List<long> paidTableIds)> CheckPaymentStatusAsync(long orderId)
    {
        var payment = await _paymentRepo.GetLatestPendingByOrderIdAsync(orderId);
        if (payment == null || string.IsNullOrEmpty(payment.TransactionId))
        {
            throw new Exception("Không tìm thấy giao dịch chờ thanh toán nào cho đơn hàng này.");
        }

        var paidTableIds = new List<long>();

        try
        {
            var paymentInfo = await _payOS.PaymentRequests.GetAsync(long.Parse(payment.TransactionId));
            if (paymentInfo.Status.ToString() == "Paid" && payment.Status == "Pending")
            {
                payment.Status = "Success";
                payment.CompletedAt = DateTime.UtcNow;
                await _paymentRepo.UpdateAsync(payment);

                paidTableIds = await _orderService.PayOrderAsync(payment.OrderId, new OrderPayDto(), paymentMethod: "PayOS");
            }
            else if (paymentInfo.Status.ToString() == "Cancelled" && payment.Status == "Pending")
            {
                payment.Status = "Failed";
                payment.CompletedAt = DateTime.UtcNow;
                await _paymentRepo.UpdateAsync(payment);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CheckPaymentStatus failed: {ex.Message}");
            throw new Exception("Không thể kiểm tra trạng thái thanh toán từ PayOS.");
        }

        return (payment, paidTableIds);
    }
}
