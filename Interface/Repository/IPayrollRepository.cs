using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IPayrollRepository
    {
        Task<List<Payroll>> GetAllAsync(long? branchId = null, int? month = null, int? year = null, long? accountId = null, List<long>? roleIds = null);
        Task<Payroll?> GetByIdAsync(long id);
        Task<List<Payroll>> GetByAccountIdAsync(long accountId, int? month = null, int? year = null);
        Task<Payroll?> GetByAccountMonthYearAsync(long accountId, int month, int year);
        Task CreateAsync(Payroll entity);
        Task UpdateAsync(Payroll entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
    }
}
