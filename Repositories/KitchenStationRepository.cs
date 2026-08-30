using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class KitchenStationRepository : IKitchenStationRepository
    {
        private readonly AppDbContext _context;

        public KitchenStationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<KitchenStation>> GetAllAsync()
        {
            return await _context.KitchenStations.Where(k => k.IsActive).ToListAsync();
        }

        public async Task<KitchenStation?> GetByIdAsync(long id)
        {
            var entity = await _context.KitchenStations.FindAsync(id);
            if (entity != null && !entity.IsActive) return null;
            return entity;
        }

        public async Task<List<KitchenStation>> GetByBranchIdAsync(long branchId)
        {
            return await _context.KitchenStations
                .Where(k => k.BranchId == branchId && k.IsActive)
                .ToListAsync();
        }

        public async Task<KitchenStation?> GetByNameAndBranchAsync(string name, long branchId)
        {
            return await _context.KitchenStations
                .FirstOrDefaultAsync(k => k.BranchId == branchId && k.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> IsUsedByAnyProductAsync(long id)
        {
            return await _context.Products.AnyAsync(p => p.KitchenStationId == id);
        }

        public async Task CreateAsync(KitchenStation entity)
        {
            await _context.KitchenStations.AddAsync(entity);
        }

        public Task UpdateAsync(KitchenStation entity)
        {
            _context.KitchenStations.Update(entity);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
