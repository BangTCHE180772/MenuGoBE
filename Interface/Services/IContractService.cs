using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Contract;

namespace MenuGoBE.Interface.Services
{
    public interface IContractService
    {
        Task<List<ContractViewDto>> GetAllAsync();
        Task<List<ContractViewDto>> GetByBranchIdsAsync(List<long> branchIds);
        Task<List<ContractViewDto>> GetContractsByRolesAsync(List<long> roleIds, long? branchId = null, List<long>? branchIds = null);
        Task<ContractViewDto?> GetByIdAsync(long id);
        Task<List<ContractViewDto>> GetByAccountIdAsync(long accountId);
        Task<ContractViewDto> CreateAsync(ContractCreateDto dto);
        Task<bool> UpdateAsync(ContractUpdateDto dto);
        Task<bool> DeleteAsync(long id);
    }
}
