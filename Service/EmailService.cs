using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using MenuGoBE.Interface.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MenuGoBE.Service
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        #region SEND Gửi email thông báo / mã OTP
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"]
                              ?? _configuration["MailSettings:Host"]
                              ?? "smtp.gmail.com";

                var portStr = _configuration["EmailSettings:Port"]
                           ?? _configuration["MailSettings:Port"]
                           ?? "587";

                var senderEmail = _configuration["EmailSettings:SenderEmail"]
                               ?? _configuration["MailSettings:Mail"];

                var senderPassword = _configuration["EmailSettings:SenderPassword"]
                                  ?? _configuration["MailSettings:Password"];

                var senderName = _configuration["EmailSettings:SenderName"]
                              ?? _configuration["MailSettings:DisplayName"]
                              ?? "MenuGo System";

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    // If SMTP credentials not configured in appsettings, log to console for development testing
                    _logger.LogInformation("================ EMAIL MOCK DISPATCH ================");
                    _logger.LogInformation($"To: {toEmail}");
                    _logger.LogInformation($"Subject: {subject}");
                    _logger.LogInformation($"Body: {body}");
                    _logger.LogInformation("======================================================");
                    return;
                }

                int port = int.TryParse(portStr, out var p) ? p : 587;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                using var message = new MailMessage();
                message.From = new MailAddress(senderEmail, senderName);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(smtpServer, port);
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(senderEmail, senderPassword);
                client.EnableSsl = true;

                await client.SendMailAsync(message);
                _logger.LogInformation($"[SMTP SUCCESS] Đã gửi email thành công tới {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[SMTP ERROR] Gặp lỗi khi gửi email tới {toEmail}: {ex.Message}");
                throw;
            }
        }
        #endregion
    }
}
