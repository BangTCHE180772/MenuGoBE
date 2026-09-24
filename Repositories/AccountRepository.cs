using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _context;

        public AccountRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Account>> GetAccountsExcludeRolesAsync(List<long> excludedRoles, List<long>? branchIds = null, long? managerAccountId = null)
        {
            var query = _context.Accounts
                .Include(a => a.Contracts)
                .AsNoTracking()
                .Where(a => !a.Contracts.Any() || !a.Contracts.Any(c => excludedRoles.Contains(c.RoleId)))
                .AsQueryable();

            if (branchIds != null && branchIds.Any())
            {
                query = query.Where(a => a.Contracts.Any(c => branchIds.Contains(c.BranchId)) || (!a.Contracts.Any() && managerAccountId.HasValue && a.CreatedBy == managerAccountId.Value));
            }

            return await query.ToListAsync();
        }

        public async Task<List<Account>> GetAccountsByRolesAsync(List<long> roleIds, long? branchId = null)
        {
            var query = _context.Accounts
                .Include(a => a.Contracts)
                .AsNoTracking()
                .Where(a => !a.Contracts.Any() || a.Contracts.Any(c => roleIds.Contains(c.RoleId)))
                .AsQueryable();

            if (branchId.HasValue)
            {
                query = query.Where(a => !a.Contracts.Any() || a.Contracts.Any(c => c.BranchId == branchId.Value));
            }

            return await query.ToListAsync();
        }

        public async Task<Account?> GetAccountByEmailAsync(string email)
        {
            return await _context.Accounts
                .Include(a => a.Contracts)
                    .ThenInclude(c => c.Role)
                .Include(a => a.TempRoles)
                    .ThenInclude(tr => tr.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Email == email && a.IsActive);
        }

        public async Task<Account?> GetAccountWithRolesAsync(long id)
        {
            return await _context.Accounts
                .Include(a => a.TempRoles)
                    .ThenInclude(tr => tr.Role)
                .Include(a => a.Contracts)
                    .ThenInclude(c => c.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Account?> GetByIdAsync(long id)
        {
            return await _context.Accounts
                .Include(a => a.Contracts)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task CreateAsync(Account entity)
        {
            await _context.Accounts.AddAsync(entity);
        }

        public Task UpdateAsync(Account entity)
        {
            _context.Accounts.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.Accounts.FindAsync(id);
            if (entity != null)
            {
                _context.Accounts.Remove(entity);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByPhoneAsync(string phone, long? excludeId = null)
        {
            return await _context.Accounts
                .AnyAsync(a => a.Phone == phone && (!excludeId.HasValue || a.Id != excludeId.Value));
        }

        public async Task<bool> ExistsByEmailAsync(string email, long? excludeId = null)
        {
            return await _context.Accounts
                .AnyAsync(a => a.Email == email && (!excludeId.HasValue || a.Id != excludeId.Value));
        }

        public async Task<bool> ExistsByCitizenIdCodeAsync(string citizenIdCode, long? excludeId = null)
        {
            if (string.IsNullOrEmpty(citizenIdCode)) return false;
            return await _context.Accounts
                .AnyAsync(a => a.CitizenIdCode == citizenIdCode && (!excludeId.HasValue || a.Id != excludeId.Value));
        }

        public async Task<List<long>> GetValidAccountIdsAsync(List<long> accountIds)
        {
            if (accountIds == null || accountIds.Count == 0) return new List<long>();
            return await _context.Accounts
                .Where(a => accountIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync();
        }

        public async Task<List<long>> GetActiveRoleIdsAsync(long accountId)
        {
            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);

            var activeContracts = await _context.Contracts
                .Where(c => c.AccountId == accountId && c.Status == "Active" && c.StartDate <= today && (c.EndDate == null || c.EndDate >= today))
                .Select(c => c.RoleId)
                .Distinct()
                .ToListAsync();

            var activeTempRoles = await _context.TempRoles
                .Where(tr => tr.AccountId == accountId && tr.Status == "Active" && tr.StartTime <= now && tr.EndTime >= now)
                .Select(tr => tr.RoleId)
                .Distinct()
                .ToListAsync();

            return activeContracts.Union(activeTempRoles).ToList();
        }

        public async Task<(bool IsAdmin, bool IsManager, List<long> BranchIds)> GetAccountPermissionsAsync(long accountId)
        {
            var activeContracts = await _context.Contracts
                .Include(c => c.Role)
                .Where(c => c.AccountId == accountId && c.Status == "Active")
                .ToListAsync();

            var activeTempRoles = await _context.TempRoles
                .Include(tr => tr.Role)
                .Where(tr => tr.AccountId == accountId && tr.Status == "Active")
                .ToListAsync();

            var allRoleIds = activeContracts.Select(c => c.RoleId)
                .Union(activeTempRoles.Select(tr => tr.RoleId))
                .ToList();

            var allRoleNames = activeContracts.Where(c => c.Role != null).Select(c => c.Role.Name)
                .Union(activeTempRoles.Where(tr => tr.Role != null).Select(tr => tr.Role.Name))
                .ToList();

            bool isAdmin = allRoleIds.Contains(1) || allRoleIds.Contains(2) ||
                           allRoleNames.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase) ||
                                                 string.Equals(r, "Owner", StringComparison.OrdinalIgnoreCase));

            bool isManager = allRoleIds.Contains(3) ||
                             allRoleNames.Any(r => string.Equals(r, "Manager", StringComparison.OrdinalIgnoreCase));

            var branchIds = activeContracts
                .Where(c => c.BranchId > 0)
                .Select(c => c.BranchId)
                .Distinct()
                .ToList();

            return (isAdmin, isManager, branchIds);
        }

        public async Task<bool> CanManagerAccessAccountAsync(long targetAccountId, List<long> managerBranchIds, long managerAccountId)
        {
            var account = await _context.Accounts
                .Include(a => a.Contracts)
                .FirstOrDefaultAsync(a => a.Id == targetAccountId);

            if (account == null) return false;
            if (!account.Contracts.Any()) return true;
            if (account.CreatedBy == managerAccountId) return true;
            if (managerBranchIds != null && managerBranchIds.Count > 0)
            {
                return account.Contracts.Any(c => managerBranchIds.Contains(c.BranchId));
            }

            return false;
        }
    }
}
