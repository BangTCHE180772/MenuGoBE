using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Account;

namespace MenuGoBE.Interface.Services
{
    public interface IAccountService
    {
        Task<List<AccountViewDto>> GetAccountsExcludeRolesAsync(List<long> excludedRoles, List<long>? branchIds = null, long? managerAccountId = null);
        Task<List<AccountViewDto>> GetAccountsByRolesAsync(List<long> roleIds, long? branchId = null);
        Task<AccountViewDto?> GetByIdAsync(long id);
        Task<AccountViewDto> CreateAsync(AccountCreateDto dto, long creatorAccountId = 0);
        Task<bool> UpdateAsync(AccountUpdateDto dto);
        Task<bool> DeleteAsync(long id);
        Task ChangePasswordAsync(long accountId, string currentPassword, string newPassword);
        Task<(bool IsAdmin, bool IsManager, List<long> BranchIds)> GetAccountPermissionsAsync(long accountId);
        Task<bool> CanManagerAccessAccountAsync(long targetAccountId, List<long> managerBranchIds, long managerAccountId);
    }
}
