using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories;

public class RecipesDetailedRepository : IRecipesDetailedRepository
{
    private readonly AppDbContext _context;

    public RecipesDetailedRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RecipesDetailed>> GetRecipesByProductIdAsync(long productId)
    {
        return await _context.RecipesDetaileds
            .Include(r => r.IngredientProduct)
            .Where(r => r.ParentProductId == productId)
            .ToListAsync();
    }

    public async Task<List<RecipesDetailed>> GetAllAsync()
    {
        return await _context.RecipesDetaileds
            .Include(r => r.IngredientProduct)
            .ToListAsync();
    }
}
