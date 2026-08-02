using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IAreaRepository
    {
        Task<List<Area>> GetAllAsync();
        Task<Area?> GetByIdAsync(long id);
        Task<List<Area>> GetByBranchIdAsync(long branchId);
        Task<Area?> GetByNameAndBranchAsync(string name, long branchId);

        Task CreateAsync(Area entity);
        Task UpdateAsync(Area entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
    }
}
