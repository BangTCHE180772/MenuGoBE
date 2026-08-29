using System.Threading.Tasks;
using MenuGoBE.Dtos.Auth;

namespace MenuGoBE.Interface.Services
{
    public interface IAuthService
    {
        Task<object> LoginAsync(LoginDto dto);

        Task<object> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<object> VerifyResetOtpAsync(VerifyResetOtpDto dto);
        Task<object> ResetPasswordAsync(ResetPasswordDto dto);
    }
}
