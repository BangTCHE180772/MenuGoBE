using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class BInventoryRepository : IBInventoryRepository
    {
        private readonly AppDbContext _context;

        public BInventoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<BInventory?> GetByProductAndBranchAsync(long productId, long branchId)
        {
            return await _context.BInventories
                .FirstOrDefaultAsync(b => b.ProductId == productId && b.BranchId == branchId);
        }

        public async Task UpdateAsync(BInventory entity)
        {
            _context.BInventories.Update(entity);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task CreateAsync(BInventory entity)
        {
            await _context.BInventories.AddAsync(entity);
        }

        public async Task BulkUpdateReservedQuantitiesAsync(System.Collections.Generic.Dictionary<string, long> reservedData)
        {
            foreach (var kvp in reservedData)
            {
                var parts = kvp.Key.Split(':');
                if (parts.Length == 2 && long.TryParse(parts[0], out long branchId) && long.TryParse(parts[1], out long productId))
                {
                    await _context.BInventories
                        .Where(b => b.BranchId == branchId && b.ProductId == productId)
                        .ExecuteUpdateAsync(s => s.SetProperty(b => b.ReservedQuantity, kvp.Value));
                }
            }
        }
    }
}
