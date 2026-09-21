using MenuGoBE.Dtos.Reservation;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.SignalR;
using MenuGoBE.Hubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MenuGoBE.Service;

public class ReservationNotificationWorker : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IReservationNotificationQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<ReservationNotificationWorker> _logger;

    public ReservationNotificationWorker(
        IReservationNotificationQueue queue,
        IServiceProvider serviceProvider,
        IHubContext<NotificationHub> hubContext,
        ILogger<ReservationNotificationWorker> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationNotificationWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            ReservationEmailNotification notification;
            try
            {
                notification = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            // 1. Gửi Email
            await SendEmailSafely(notification);

            // 2. Bắn SignalR cho khách (nếu đang online)
            await SendSignalRSafely(notification);
        }

        _logger.LogInformation("ReservationNotificationWorker stopped.");
    }

    private async Task SendEmailSafely(ReservationEmailNotification n)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(n.CustomerEmail))
            {
                _logger.LogWarning("[Email] Bỏ qua: Khách {Name} (ID={Id}) không có email. Event={Event}",
                    n.CustomerName, n.CustomerId, n.EventType);
                return;
            }

            var (subject, body) = BuildEmailContent(n);

            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            await emailService.SendEmailAsync(n.CustomerEmail, subject, body);

            _logger.LogInformation("[Email] Đã gửi thành công tới {Email}. Event={Event}, ReservationId={Id}",
                n.CustomerEmail, n.EventType, n.ReservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] Lỗi gửi email cho {Email}. Event={Event}, ReservationId={Id}. Bỏ qua.",
                n.CustomerEmail, n.EventType, n.ReservationId);
            // KHÔNG throw - Worker tiếp tục xử lý notification tiếp theo
        }
    }

    private async Task SendSignalRSafely(ReservationEmailNotification n)
    {
        try
        {
            if (n.CustomerId <= 0) return;

            var signalRType = n.EventType switch
            {
                "Created" => "reservation-created",
                "Confirmed" => "reservation-confirmed",
                "CheckedIn" => "reservation-checkedin",
                "Cancelled" => "reservation-cancelled",
                "Rejected" => "reservation-rejected",
                "Reminder30Min" => "reservation-reminder",
                "LateWarning15Min" => "reservation-late-warning",
                "AutoCancelled30Min" => "reservation-auto-cancelled",
                _ => "reservation-update"
            };

            var (_, bodyText) = BuildEmailContent(n);
            // Strip HTML for SignalR message
            var plainMessage = System.Text.RegularExpressions.Regex.Replace(bodyText, "<[^>]+>", "");
            if (plainMessage.Length > 200) plainMessage = plainMessage[..200] + "...";

            await _hubContext.Clients.Group($"customer_{n.CustomerId}").SendAsync("ReceiveNotification", new
            {
                type = signalRType,
                message = GetShortMessage(n),
                reservationId = n.ReservationId,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SignalR] Lỗi bắn thông báo cho customer_{Id}. Bỏ qua.", n.CustomerId);
        }
    }

    private static string GetShortMessage(ReservationEmailNotification n)
    {
        var timeStr = n.ReservationTime.AddHours(7).ToString("HH:mm dd/MM/yyyy");
        return n.EventType switch
        {
            "Created" => $"Yêu cầu đặt bàn lúc {timeStr} tại {n.BranchName} đã được gửi thành công.",
            "Confirmed" => $"Đặt bàn lúc {timeStr} tại {n.BranchName} đã được xác nhận!",
            "CheckedIn" => $"Check-in thành công. Chúc bạn có bữa ăn ngon miệng!",
            "Cancelled" => $"Đặt bàn lúc {timeStr} tại {n.BranchName} đã bị hủy.",
            "Rejected" => $"Đặt bàn lúc {timeStr} tại {n.BranchName} đã bị từ chối.",
            "Reminder30Min" => $"Nhắc lịch: Bạn có bàn đặt lúc {timeStr} tại {n.BranchName} (còn ~30 phút).",
            "LateWarning15Min" => $"Bạn đã trễ 15 phút so với lịch đặt bàn lúc {timeStr}. Vui lòng đến sớm!",
            "AutoCancelled30Min" => $"Đặt bàn lúc {timeStr} tại {n.BranchName} đã tự động hủy do quá 30 phút không check-in.",
            _ => $"Cập nhật đặt bàn tại {n.BranchName}."
        };
    }

    private static (string Subject, string Body) BuildEmailContent(ReservationEmailNotification n)
    {
        var timeStr = n.ReservationTime.AddHours(7).ToString("HH:mm - dd/MM/yyyy");
        var tableInfo = string.IsNullOrEmpty(n.TableNames) ? "" : $"<p><strong>Bàn:</strong> {n.TableNames}</p>";

        string subject;
        string heading;
        string message;
        string accentColor;

        switch (n.EventType)
        {
            case "Created":
                subject = "MenuGo - Xác nhận yêu cầu đặt bàn";
                heading = "Yêu cầu đặt bàn đã được gửi";
                message = "Cảm ơn bạn đã đặt bàn tại MenuGo! Yêu cầu của bạn đã được gửi thành công và đang chờ nhà hàng xác nhận.";
                accentColor = "#f97316";
                break;
            case "Confirmed":
                subject = "MenuGo - Đặt bàn đã được xác nhận";
                heading = "Đặt bàn đã được xác nhận!";
                message = "Tuyệt vời! Nhà hàng đã xác nhận lịch đặt bàn của bạn. Hẹn gặp bạn tại nhà hàng!";
                accentColor = "#22c55e";
                break;
            case "CheckedIn":
                subject = "MenuGo - Check-in thành công";
                heading = "Check-in thành công!";
                message = "Bạn đã nhận bàn thành công. Chúc bạn có bữa ăn ngon miệng!";
                accentColor = "#3b82f6";
                break;
            case "Cancelled":
                subject = "MenuGo - Đặt bàn đã bị hủy";
                heading = "Đặt bàn đã bị hủy";
                message = "Lịch đặt bàn của bạn đã bị hủy. Nếu bạn có thắc mắc, vui lòng liên hệ nhà hàng.";
                accentColor = "#ef4444";
                break;
            case "Rejected":
                subject = "MenuGo - Đặt bàn bị từ chối";
                heading = "Đặt bàn bị từ chối";
                message = "Rất tiếc, nhà hàng không thể đáp ứng yêu cầu đặt bàn của bạn vào thời điểm này. Vui lòng thử lại với thời gian khác.";
                accentColor = "#ef4444";
                break;
            case "Reminder30Min":
                subject = "MenuGo - Nhắc lịch đặt bàn";
                heading = "Sắp đến giờ đặt bàn!";
                message = "Chỉ còn khoảng 30 phút nữa là đến giờ đặt bàn của bạn. Hãy chuẩn bị để có mặt đúng giờ nhé!";
                accentColor = "#f97316";
                break;
            case "LateWarning15Min":
                subject = "MenuGo - Nhắc nhở: Bạn đã trễ 15 phút";
                heading = "Bạn đã trễ 15 phút!";
                message = "Bàn của bạn đang được giữ nhưng thời gian giữ bàn có hạn. Vui lòng đến nhà hàng sớm nhất có thể để tránh bị hủy tự động.";
                accentColor = "#f59e0b";
                break;
            case "AutoCancelled30Min":
                subject = "MenuGo - Đặt bàn đã tự động hủy";
                heading = "Đặt bàn đã tự động hủy";
                message = "Do quá 30 phút kể từ giờ hẹn mà bạn chưa đến check-in, hệ thống đã tự động hủy đặt bàn. Nếu bạn vẫn muốn dùng bữa, vui lòng đặt bàn lại.";
                accentColor = "#dc2626";
                break;
            default:
                subject = "MenuGo - Cập nhật đặt bàn";
                heading = "Cập nhật đặt bàn";
                message = "Có cập nhật mới cho đơn đặt bàn của bạn.";
                accentColor = "#6b7280";
                break;
        }

        var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""></head>
<body style=""margin:0;padding:0;background:#f4f4f5;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">
  <div style=""max-width:560px;margin:24px auto;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.06);"">
    <div style=""background:{accentColor};padding:24px 32px;"">
      <h1 style=""color:#ffffff;margin:0;font-size:20px;"">{heading}</h1>
    </div>
    <div style=""padding:28px 32px;"">
      <p style=""color:#374151;font-size:15px;line-height:1.6;margin:0 0 20px;"">{message}</p>
      <div style=""background:#f9fafb;border-radius:8px;padding:16px 20px;margin-bottom:20px;"">
        <p style=""margin:4px 0;""><strong>Khách hàng:</strong> {n.CustomerName}</p>
        <p style=""margin:4px 0;""><strong>Thời gian:</strong> {timeStr}</p>
        <p style=""margin:4px 0;""><strong>Chi nhánh:</strong> {n.BranchName}</p>
        {tableInfo}
      </div>
      <p style=""color:#9ca3af;font-size:12px;margin:0;"">Email này được gửi tự động từ hệ thống MenuGo. Vui lòng không trả lời email này.</p>
    </div>
  </div>
</body>
</html>";

        return (subject, body);
    }
}
