using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Branch;

namespace MenuGoBE.Interface.Services
{
    public interface IBranchService
    {
        Task<List<BranchViewDto>> GetAllAsync();

        Task<BranchViewDto?> GetByIdAsync(long id);

        Task<BranchViewDto> CreateAsync(BranchCreateDto dto);

        Task<bool> UpdateAsync(BranchUpdateDto dto);

        Task<bool> DeleteAsync(long id);
    }
}