using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Account;

namespace MenuGoBE.Interface.Services
{
    public interface IAccountService
    {
        Task<List<AccountViewDto>> GetAccountsWithRoleRangeAsync(int minRole, int maxRole);
        Task<AccountViewDto?> GetByIdAsync(long id);
        Task<AccountViewDto> CreateAsync(AccountCreateDto dto);
        Task<bool> UpdateAsync(AccountUpdateDto dto);
        Task<bool> DeleteAsync(long id);
    }
}
