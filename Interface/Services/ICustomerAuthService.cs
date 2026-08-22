using System.Threading.Tasks;
using MenuGoBE.Dtos.Auth;

namespace MenuGoBE.Interface.Services
{
    public interface ICustomerAuthService
    {
        Task<bool> RegisterAsync(CustomerRegisterDto dto);
        Task<CustomerLoginResponseDto> LoginAsync(CustomerLoginDto dto);
        Task<bool> SendResetPasswordOtpAsync(string email);
        Task<bool> ResetPasswordWithOtpAsync(CustomerResetPasswordDto dto);
    }
}
