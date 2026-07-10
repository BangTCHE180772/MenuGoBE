using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IUnitRepository
    {
        Task<List<Unit>> GetAllAsync();
        Task<Unit?> GetByIdAsync(long id);
        Task CreateAsync(Unit entity);
        Task UpdateAsync(Unit entity);
        Task DeleteAsync(long id);
        Task<bool> ExistsByNameAsync(string name);
        Task<bool> ExistsByNameExcludeIdAsync(string name, long id);
        Task<bool> HasConversionsAsync(long id);
        Task SaveChangesAsync();
    }
}
