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
        Task<Table?> GetByIdAsync(long id);

        Task CreateAsync(Table entity);
        Task UpdateAsync(Table entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
    }
}
