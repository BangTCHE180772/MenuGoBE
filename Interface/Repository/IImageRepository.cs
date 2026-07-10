using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IImageRepository
    {
        Task<List<Models.Image>> GetAllAsync();
        Task<Models.Image?> GetByIdAsync(long id);
        Task CreateAsync(Models.Image entity);
        Task UpdateAsync(Models.Image entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
    }
}
