using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface ITableRepository
    {
        Task<List<Table>> GetAllAsync();
        Task<List<Table>> GetByBranchIdsAsync(List<long> branchIds);
        Task<Table?> GetByIdAsync(long id);
        Task<List<Table>> GetByIdsAsync(IEnumerable<long> ids);
        Task<Table?> GetByNameAndAreaAsync(string name, long areaId);

        Task CreateAsync(Table entity);
        Task UpdateAsync(Table entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
    }
}
