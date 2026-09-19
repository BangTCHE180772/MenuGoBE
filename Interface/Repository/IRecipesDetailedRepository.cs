using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository;

public interface IRecipesDetailedRepository
{
    Task<List<RecipesDetailed>> GetRecipesByProductIdAsync(long productId);
    Task<List<RecipesDetailed>> GetAllAsync();
}
