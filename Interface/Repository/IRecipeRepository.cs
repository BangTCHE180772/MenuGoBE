using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IRecipeRepository
    {
        Task<List<Recipe>> GetAllAsync();
        Task<Recipe?> GetByIdAsync(long id);
        Task<Recipe?> GetActiveByProductIdAsync(long productId);
        Task CreateAsync(Recipe entity);
        Task UpdateAsync(Recipe entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
    }
}
