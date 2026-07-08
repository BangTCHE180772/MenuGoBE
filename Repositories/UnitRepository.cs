using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class UnitRepository : IUnitRepository
    {
        private readonly AppDbContext _context;

        public UnitRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Unit>> GetAllAsync()
        {
            return await _context.Units.ToListAsync();
        }

        public async Task<Unit?> GetByIdAsync(long id)
        {
            return await _context.Units.FindAsync(id);
        }

        public async Task CreateAsync(Unit entity)
        {
            await _context.Units.AddAsync(entity);
        }

        public Task UpdateAsync(Unit entity)
        {
            _context.Units.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.Units.FindAsync(id);
            if (entity != null)
                _context.Units.Remove(entity);
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Units.AnyAsync(u => u.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> ExistsByNameExcludeIdAsync(string name, long id)
        {
            return await _context.Units.AnyAsync(u => u.Name.ToLower() == name.ToLower() && u.Id != id);
        }

        public async Task<bool> HasConversionsAsync(long id)
        {
            return await _context.UnitConversions.AnyAsync(uc => uc.UnitId == id);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
