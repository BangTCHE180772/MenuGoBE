using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class GroupRepository : IGroupRepository
    {
        private readonly AppDbContext _context;

        public GroupRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Group>> GetAllAsync()
        {
            return await _context.Groups.ToListAsync();
        }

        public async Task<List<Group>> GetSellableAsync(long? branchId = null)
        {
            var validTypes = new[] { ProductType.Processed, ProductType.Manufactured, ProductType.Regular };

            var query = _context.Groups.AsQueryable();

            if (branchId.HasValue)
            {
                query = query.Where(g => g.Products.Any(p => 
                    p.IsSellable && 
                    validTypes.Contains(p.Type) &&
                    p.BInventories.Any(bi => bi.BranchId == branchId.Value && bi.BranchActive && bi.ChainActive)
                ));
            }
            else
            {
                query = query.Where(g => g.Products.Any(p => 
                    p.IsSellable && 
                    validTypes.Contains(p.Type)
                ));
            }

            return await query.ToListAsync();
        }

        public async Task<Group?> GetByIdAsync(long id)
        {
            return await _context.Groups.FindAsync(id);
        }

        public async Task CreateAsync(Group entity)
        {
            await _context.Groups.AddAsync(entity);
        }

        public Task UpdateAsync(Group entity)
        {
            _context.Groups.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.Groups.FindAsync(id);
            if (entity != null)
                _context.Groups.Remove(entity);
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Groups.AnyAsync(g => g.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> ExistsByNameExcludeIdAsync(string name, long id)
        {
            return await _context.Groups.AnyAsync(g => g.Name.ToLower() == name.ToLower() && g.Id != id);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
