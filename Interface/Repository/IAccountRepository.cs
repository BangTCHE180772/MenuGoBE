using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IAccountRepository
    {
        Task<List<Account>> GetAccountsExcludeRolesAsync(List<long> excludedRoles);
        Task<Account?> GetByIdAsync(long id);
        Task<Account?> GetAccountByEmailAsync(string email);
        Task<Account?> GetAccountWithRolesAsync(long id);
        Task CreateAsync(Account entity);
        Task UpdateAsync(Account entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
        Task<bool> ExistsByPhoneAsync(string phone, long? excludeId = null);
        Task<bool> ExistsByEmailAsync(string email, long? excludeId = null);
        Task<bool> ExistsByCitizenIdCodeAsync(string citizenIdCode, long? excludeId = null);
    }
}
