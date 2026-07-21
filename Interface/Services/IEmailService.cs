using System.Threading.Tasks;

namespace MenuGoBE.Interface.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
