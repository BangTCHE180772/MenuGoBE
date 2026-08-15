using MenuGoBE.Dtos.Device;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Services
{
    public interface IDeviceService
    {
        Task<object> SetupAsync(DeviceSetupDto dto, long branchId, long accountId);
        Task<object?> ValidateAsync(DeviceValidateDto dto);
        Task<List<object>> GetByBranchIdAsync(long branchId);
        Task UpdateAsync(DeviceUpdateDto dto);
        Task DeleteAsync(long id);
        Task RevokeAsync(long id);
        Task<object?> EmployeeLoginAsync(DeviceEmployeeLoginDto dto);
        Task<object?> PersonalLoginAsync(string deviceToken, string email);
        Task<Device?> GetDeviceByTokenAsync(string token);
    }
}
