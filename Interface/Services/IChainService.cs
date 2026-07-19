using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Chain;

namespace MenuGoBE.Interface.Services
{
    public interface IChainService
    {
        Task<List<ChainViewDto>> GetAllAsync();
        Task<ChainViewDto?> GetMainChainAsync();
        Task<ChainViewDto?> GetByIdAsync(long id);

        Task<ChainViewDto> CreateAsync(ChainCreateDto dto);
        Task<bool> UpdateAsync(ChainUpdateDto dto);
        Task<bool> DeleteAsync(long id);
    }
}