using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IBInventoryRepository
    {
        Task<BInventory?> GetByProductAndBranchAsync(long productId, long branchId);
        Task UpdateAsync(BInventory entity);
        Task SaveChangesAsync();
        Task CreateAsync(BInventory entity);
        Task BulkUpdateReservedQuantitiesAsync(System.Collections.Generic.Dictionary<string, long> reservedData);
    }
}
