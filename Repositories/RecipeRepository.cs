using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class RecipeRepository : IRecipeRepository
    {
        private readonly AppDbContext _context;

        public RecipeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Recipe>> GetAllAsync()
        {
            return await _context.Recipes.ToListAsync();
        }

        public async Task<Recipe?> GetByIdAsync(long id)
        {
            return await _context.Recipes.FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Recipe?> GetActiveByProductIdAsync(long productId)
        {
            return await _context.Recipes
                .Where(r => r.ProductId == productId && r.IsActive)
                .FirstOrDefaultAsync();
        }

        public async Task CreateAsync(Recipe entity)
        {
            await _context.Recipes.AddAsync(entity);
        }

        public async Task UpdateAsync(Recipe entity)
        {
            _context.Recipes.Update(entity);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.Recipes.FindAsync(id);
            if (entity != null)
            {
                _context.Recipes.Remove(entity);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
