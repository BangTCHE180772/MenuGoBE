using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IContractRepository
    {
        Task<List<Contract>> GetAllAsync();
        Task<Contract?> GetByIdAsync(long id);
        Task CreateAsync(Contract entity);
        Task UpdateAsync(Contract entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
    }
}
