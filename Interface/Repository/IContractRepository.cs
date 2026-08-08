using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IContractRepository
    {
        Task<List<Contract>> GetAllAsync();
        Task<List<Contract>> GetByBranchIdsAsync(List<long> branchIds);
        Task<List<Contract>> GetContractsByRolesAsync(List<long> roleIds, long? branchId = null, List<long>? branchIds = null);
        Task<Contract?> GetByIdAsync(long id);
        Task<List<Contract>> GetByAccountIdAsync(long accountId);
        Task CreateAsync(Contract entity);
        Task UpdateAsync(Contract entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
        Task<bool> HasActiveManagerAsync(long branchId, long excludeContractId = 0);
        Task<bool> IsManagerRoleAsync(long roleId);
        Task<bool> HasActiveContractByAccountAsync(long accountId, long excludeContractId = 0);
        Task UpdateExpiredContractsAsync();
    }
}
