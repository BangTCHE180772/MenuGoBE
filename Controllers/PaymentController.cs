using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;
using Microsoft.AspNetCore.SignalR;
using MenuGoBE.Hubs;

namespace MenuGoBE.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public PaymentController(IPaymentService paymentService, IHubContext<NotificationHub> hubContext)
    {
        _paymentService = paymentService;
        _hubContext = hubContext;
    }

    [HttpPost("create-payment-link/{orderId}")]
    public async Task<IActionResult> CreatePaymentLink(long orderId)
    {
        try
        {
            var domain = $"{Request.Scheme}://{Request.Host}";
            var payment = await _paymentService.CreatePaymentLinkAsync(orderId, $"{domain}/cancel", $"{domain}/success");
            
            return Ok(new
            {
                checkoutUrl = payment.CheckoutUrl,
                qrCode = payment.QrCode,
                paymentLinkId = payment.PaymentLinkId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] Webhook body)
    {
        var (verified, paidTableIds) = await _paymentService.VerifyPaymentWebhookAsync(body);

        if (verified)
        {
            foreach (var tableId in paidTableIds)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    TableId = tableId,
                    Type = "Payment",
                    Message = "Thanh toán qua PayOS thành công. Bàn cần dọn dẹp!"
                });
            }

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                Type = "PaymentWebhook",
                Message = "Thanh toán qua mã QR PayOS thành công!"
            });
            return Ok(new { success = true });
        }

        return BadRequest(new { success = false });
    }

    [HttpGet("check-payment/{orderId}")]
    public async Task<IActionResult> CheckPaymentStatus(long orderId)
    {
        try
        {
            var (payment, paidTableIds) = await _paymentService.CheckPaymentStatusAsync(orderId);

            if (payment.Status == "Success" && paidTableIds.Any())
            {
                foreach (var tableId in paidTableIds)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                    {
                        TableId = tableId,
                        Type = "Payment",
                        Message = "Thanh toán qua PayOS thành công. Bàn cần dọn dẹp!"
                    });
                }
            }

            return Ok(new
            {
                success = true,
                status = payment.Status,
                message = payment.Status == "Success" ? "Thanh toán thành công!" : "Chưa thanh toán hoặc đã huỷ."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
