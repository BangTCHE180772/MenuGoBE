using MenuGoBE.Models;
using PayOS.Models.Webhooks;

namespace MenuGoBE.Interface.Services;

public interface IPaymentService
{
    Task<Payment> CreatePaymentLinkAsync(long orderId, string cancelUrl, string returnUrl);
    Task<(bool verified, List<long> paidTableIds)> VerifyPaymentWebhookAsync(Webhook webhookBody);
    Task<(Payment payment, List<long> paidTableIds)> CheckPaymentStatusAsync(long orderId);
}
