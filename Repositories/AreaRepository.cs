using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class AreaRepository : IAreaRepository
    {
        private readonly AppDbContext _context;

        public AreaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Area>> GetAllAsync()
        {
            return await _context.Areas.Where(a => a.IsActive).ToListAsync();
        }

        public async Task<Area?> GetByIdAsync(long id)
        {
            var entity = await _context.Areas.FindAsync(id);
            if (entity != null && !entity.IsActive) return null;
            return entity;
        }

        public async Task<List<Area>> GetByBranchIdAsync(long branchId)
        {
            return await _context.Areas
                .Where(a => a.BranchId == branchId && a.IsActive)
                .ToListAsync();
        }

        public async Task<Area?> GetByNameAndBranchAsync(string name, long branchId)
        {
            return await _context.Areas
                .FirstOrDefaultAsync(a => a.BranchId == branchId && a.Name.ToLower() == name.ToLower());
        }

        public async Task CreateAsync(Area entity)
        {
            await _context.Areas.AddAsync(entity);
        }

        public Task UpdateAsync(Area entity)
        {
            _context.Areas.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.Areas.FindAsync(id);
            if (entity != null)
            {
                _context.Areas.Remove(entity);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
