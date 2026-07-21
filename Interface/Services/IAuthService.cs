using System.Threading.Tasks;
using MenuGoBE.Dtos.Auth;

namespace MenuGoBE.Interface.Services
{
    public interface IAuthService
    {
        Task<object> LoginAsync(LoginDto dto);
        Task<object> Setup2FAAsync(Setup2FADto dto);
        Task<object> ConfirmSetup2FAAsync(ConfirmSetup2FADto dto);
        Task<object> Verify2FAAsync(Verify2FADto dto);
    }
}
