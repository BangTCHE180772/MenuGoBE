using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;
using Microsoft.AspNetCore.SignalR;
using MenuGoBE.Hubs;
using MenuGoBE.Dtos.Order;

namespace MenuGoBE.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IOrderAssignmentService _assignmentService;

    public PaymentController(IPaymentService paymentService, IHubContext<NotificationHub> hubContext, IOrderAssignmentService assignmentService)
    {
        _paymentService = paymentService;
        _hubContext = hubContext;
        _assignmentService = assignmentService;
    }

    [HttpPost("create-payment-link/{orderId}")]
    public async Task<IActionResult> CreatePaymentLink(long orderId, [FromBody] OrderPayDto? dto = null)
    {
        try
        {
            var domain = $"{Request.Scheme}://{Request.Host}";
            var payment = await _paymentService.CreatePaymentLinkAsync(orderId, $"{domain}/cancel", $"{domain}/success", dto);
            
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
        var (verified, paidTableIds, orderId) = await _paymentService.VerifyPaymentWebhookAsync(body);

        if (verified)
        {
            await _assignmentService.DeactivateAssignmentAsync(orderId, "Paid");

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
                OrderId = orderId,
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

            if (payment.Status == "Success")
            {
                await _assignmentService.DeactivateAssignmentAsync(orderId, "Paid");
                
                if (paidTableIds.Any())
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
