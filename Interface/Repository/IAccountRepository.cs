using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IAccountRepository
    {
        Task<List<Account>> GetAccountsWithRoleRangeAsync(int minRole, int maxRole);
        Task<Account?> GetByIdAsync(long id);
        Task<Account?> GetAccountByEmailAsync(string email);
        Task<Account?> GetAccountWithRolesAsync(long id);
        Task CreateAsync(Account entity);
        Task UpdateAsync(Account entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
    }
}
