using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IKitchenStationRepository
    {
        Task<List<KitchenStation>> GetAllAsync();
        Task<KitchenStation?> GetByIdAsync(long id);
        Task<List<KitchenStation>> GetByBranchIdAsync(long branchId);
        Task<KitchenStation?> GetByNameAndBranchAsync(string name, long branchId);
        Task<bool> IsUsedByAnyProductAsync(long id);

        Task CreateAsync(KitchenStation entity);
        Task UpdateAsync(KitchenStation entity);
        Task SaveChangesAsync();
    }
}
