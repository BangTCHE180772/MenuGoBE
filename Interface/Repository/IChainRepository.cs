using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Models;
namespace MenuGoBE.Interface.Repository
{
    public interface IChainRepository
    {
        Task<List<Chain>> GetAllAsync();
        Task<Chain?> GetMainChainAsync();
        Task<Chain?> GetByIdAsync(long id);

        Task CreateAsync(Chain entity);
        Task UpdateAsync(Chain entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
    }
}