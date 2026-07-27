using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface ISalaryDetailRepository
    {
        Task<List<SalaryDetail>> GetAllAsync(long? payrollId = null, long? accountId = null, List<long>? branchIds = null, List<long>? roleIds = null);
        Task<SalaryDetail?> GetByIdAsync(long id);
        Task CreateAsync(SalaryDetail entity);
        Task UpdateAsync(SalaryDetail entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
    }
}
