using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IGroupRepository
    {
        Task<List<Group>> GetAllAsync();
        Task<List<Group>> GetSellableAsync(long? branchId = null);
        Task<Group?> GetByIdAsync(long id);
        Task CreateAsync(Group entity);
        Task UpdateAsync(Group entity);
        Task DeleteAsync(long id);
        Task<bool> ExistsByNameAsync(string name);
        Task<bool> ExistsByNameExcludeIdAsync(string name, long id);
        Task SaveChangesAsync();
    }
}
