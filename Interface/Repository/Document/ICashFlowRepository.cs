using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository.Document
{
    public interface ICashFlowRepository
    {
        Task<IEnumerable<CashFlow>> GetByBranchIdAsync(long? branchId, DateTime? startDate = null, DateTime? endDate = null);

        Task<CashFlow?> GetByIdAsync(long id);

        Task AddAsync(CashFlow cashFlow);

        void Update(CashFlow cashFlow);

        Task<bool> SaveChangesAsync();
    }
}
