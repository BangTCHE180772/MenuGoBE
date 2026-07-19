using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IDeviceRepository
    {
        Task<Device?> GetByTokenAsync(string token);
        Task<Device?> GetByIdAsync(long id);
        Task<List<Device>> GetByBranchIdAsync(long branchId);
        Task CreateAsync(Device entity);
        Task UpdateAsync(Device entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
    }
}
