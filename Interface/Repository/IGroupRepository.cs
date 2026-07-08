using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IGroupRepository
    {
        Task<List<Group>> GetAllAsync();
        Task<Group?> GetByIdAsync(long id);
        Task CreateAsync(Group entity);
        Task UpdateAsync(Group entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
    }
}
