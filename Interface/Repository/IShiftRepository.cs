using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IShiftRepository
    {
        Task<List<Shift>> GetAllAsync();
        Task<Shift?> GetByIdAsync(long id);
        Task CreateAsync(Shift entity);
        Task UpdateAsync(Shift entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
    }
}
