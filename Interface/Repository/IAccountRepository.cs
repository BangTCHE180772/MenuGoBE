using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IAccountRepository
    {
        Task<List<Account>> GetAccountsWithRoleRangeAsync(int minRole, int maxRole);
    }
}
