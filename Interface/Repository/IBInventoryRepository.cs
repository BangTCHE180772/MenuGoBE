using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IBInventoryRepository
    {
        Task<BInventory?> GetByProductAndBranchAsync(long productId, long branchId);
        Task<List<BInventory>> GetAllByBranchAsync(long branchId);
        Task UpdateAsync(BInventory entity);
        Task SaveChangesAsync();
        Task CreateAsync(BInventory entity);
    }
}
