using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Branch;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IBranchRepository
    {
        Task<List<Branch>> GetAllAsync();

        Task<Branch?> GetByIdAsync(long id);

        Task<List<Branch>> SearchFilteredBranchesAsync(BranchQueryDto dto);

        Task CreateAsync(Branch branch);

        Task UpdateAsync(Branch branch);

        Task DeleteAsync(long id);

        Task SaveChangesAsync();
    }
}