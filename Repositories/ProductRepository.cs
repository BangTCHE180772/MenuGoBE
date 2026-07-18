using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Product?> GetByIdAsync(long id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<List<Product>> GetByTypeAsync(string type)
        {
            return await _context.Products
                .Include(p => p.UnitConversions)
                .Include(p => p.Recipes)
                    .ThenInclude(r => r.RecipesDetaileds)
                .Where(p => p.Type.ToLower() == type.ToLower())
                .ToListAsync();
        }

        public async Task<Product?> GetByIdAndTypeAsync(long id, string type)
        {
            return await _context.Products
                .Include(p => p.UnitConversions)
                .Include(p => p.Recipes)
                    .ThenInclude(r => r.RecipesDetaileds)
                .FirstOrDefaultAsync(p => p.Id == id && p.Type.ToLower() == type.ToLower());
        }

        public async Task CreateAsync(Product entity)
        {
            await _context.Products.AddAsync(entity);
        }

        public Task UpdateAsync(Product entity)
        {
            _context.Products.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Product entity)
        {
            _context.Products.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Products.AnyAsync(p => p.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> ExistsByNameExcludeIdAsync(string name, long id)
        {
            return await _context.Products.AnyAsync(p => p.Name.ToLower() == name.ToLower() && p.Id != id);
        }

        public async Task<bool> ExistsBySKUAsync(string sku)
        {
            return await _context.Products.AnyAsync(p => p.SKUCode.ToLower() == sku.ToLower());
        }

        public async Task<bool> ExistsBySKUExcludeIdAsync(string sku, long id)
        {
            return await _context.Products.AnyAsync(p => p.SKUCode.ToLower() == sku.ToLower() && p.Id != id);
        }

        public async Task<bool> UnitExistsAsync(long unitId)
        {
            return await _context.Units.AnyAsync(u => u.Id == unitId);
        }

        public async Task ClearUnitConversionsAsync(long productId)
        {
            var conversions = await _context.UnitConversions
                .Where(uc => uc.ProductId == productId)
                .ToListAsync();
            _context.UnitConversions.RemoveRange(conversions);
        }

        public async Task<string?> GetProductTypeAsync(long id)
        {
            var p = await _context.Products.FindAsync(id);
            return p?.Type;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
