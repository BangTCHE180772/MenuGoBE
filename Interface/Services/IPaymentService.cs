using MenuGoBE.Models;
using PayOS.Models.Webhooks;
using MenuGoBE.Dtos.Order;

namespace MenuGoBE.Interface.Services;

public interface IPaymentService
{
    Task<Payment> CreatePaymentLinkAsync(long orderId, string cancelUrl, string returnUrl, OrderPayDto? dto = null);
    Task<(bool verified, List<long> paidTableIds, long orderId)> VerifyPaymentWebhookAsync(Webhook webhookBody);
    Task<(Payment payment, List<long> paidTableIds)> CheckPaymentStatusAsync(long orderId);
}
