using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class MenuRepository : IMenuRepository
    {
        private readonly AppDbContext _context;

        public MenuRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Menu>> GetAllAsync()
        {
            return await _context.Menus.ToListAsync();
        }

        public async Task<Menu?> GetByIdAsync(long id)
        {
            return await _context.Menus.FindAsync(id);
        }

        /// <summary>Lấy menu kèm danh sách MenuProducts.</summary>
        public async Task<Menu?> GetByIdWithProductsAsync(long id)
        {
            return await _context.Menus
                .Include(m => m.MenuProducts)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task CreateAsync(Menu entity)
        {
            await _context.Menus.AddAsync(entity);
        }

        public Task UpdateAsync(Menu entity)
        {
            _context.Menus.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.Menus.FindAsync(id);
            if (entity != null)
                _context.Menus.Remove(entity);
        }

        // ── Single product ───────────────────────────────────────────────────

        public async Task<bool> ProductExistsInMenuAsync(long menuId, long productId)
        {
            return await _context.MenuProducts
                .AnyAsync(mp => mp.MenuId == menuId && mp.ProductId == productId);
        }

        public async Task AddProductAsync(long menuId, long productId)
        {
            var link = new MenuProduct { MenuId = menuId, ProductId = productId };
            await _context.MenuProducts.AddAsync(link);
        }

        public async Task RemoveProductAsync(long menuId, long productId)
        {
            var link = await _context.MenuProducts
                .FirstOrDefaultAsync(mp => mp.MenuId == menuId && mp.ProductId == productId);
            if (link != null)
                _context.MenuProducts.Remove(link);
        }

        // ── Bulk products ────────────────────────────────────────────────────

        public async Task AddProductsAsync(long menuId, IEnumerable<long> productIds)
        {
            var existing = await _context.MenuProducts
                .Where(mp => mp.MenuId == menuId)
                .Select(mp => mp.ProductId)
                .ToListAsync();

            var toAdd = productIds
                .Distinct()
                .Except(existing)
                .Select(pid => new MenuProduct { MenuId = menuId, ProductId = pid });

            await _context.MenuProducts.AddRangeAsync(toAdd);
        }

        public async Task RemoveProductsAsync(long menuId, IEnumerable<long> productIds)
        {
            var ids = productIds.Distinct().ToList();
            var links = await _context.MenuProducts
                .Where(mp => mp.MenuId == menuId && ids.Contains(mp.ProductId))
                .ToListAsync();

            _context.MenuProducts.RemoveRange(links);
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Menus.AnyAsync(m => m.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> ExistsByNameExcludeIdAsync(string name, long id)
        {
            return await _context.Menus.AnyAsync(m => m.Name.ToLower() == name.ToLower() && m.Id != id);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
