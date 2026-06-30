using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Account;

namespace MenuGoBE.Interface.Services
{
    public interface IAccountService
    {
        Task<List<AccountViewDto>> GetAccountsWithRoleRangeAsync(int minRole, int maxRole);
    }
}
