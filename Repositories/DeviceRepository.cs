using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class DeviceRepository : IDeviceRepository
    {
        private readonly AppDbContext _context;

        public DeviceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Device?> GetByTokenAsync(string token)
        {
            return await _context.Devices
                .Include(d => d.Branch)
                .FirstOrDefaultAsync(d => d.Token == token);
        }

        public async Task<Device?> GetByIdAsync(long id)
        {
            return await _context.Devices.FindAsync(id);
        }

        public async Task<List<Device>> GetByBranchIdAsync(long branchId)
        {
            return await _context.Devices
                .Where(d => d.BranchId == branchId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task CreateAsync(Device entity)
        {
            await _context.Devices.AddAsync(entity);
            await SaveChangesAsync();
        }

        public async Task UpdateAsync(Device entity)
        {
            _context.Devices.Update(entity);
            await SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _context.Devices.Remove(entity);
                await SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
