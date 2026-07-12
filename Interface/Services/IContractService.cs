using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Contract;

namespace MenuGoBE.Interface.Services
{
    public interface IContractService
    {
        Task<List<ContractViewDto>> GetAllAsync();
        Task<ContractViewDto?> GetByIdAsync(long id);
        Task<ContractViewDto> CreateAsync(ContractCreateDto dto);
        Task<bool> UpdateAsync(ContractUpdateDto dto);
        Task<bool> DeleteAsync(long id);
    }
}
