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

        public async Task<List<Account>> GetAccountsExcludeRolesAsync(List<long> excludedRoles)
        {
            return await _context.Accounts
                .Include(a => a.Contracts)
                .Where(a => !a.Contracts.Any(c => excludedRoles.Contains(c.RoleId)))
                .ToListAsync();
        }

        public async Task<Account?> GetAccountByEmailAsync(string email)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(a => a.Email == email && a.IsActive);
        }

        public async Task<Account?> GetAccountWithRolesAsync(long id)
        {
            return await _context.Accounts
                .Include(a => a.TempRoles)
                    .ThenInclude(tr => tr.Role)
                .Include(a => a.Contracts)
                    .ThenInclude(c => c.Role)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Account?> GetByIdAsync(long id)
        {
            return await _context.Accounts
                .Include(a => a.Contracts)
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
    }
}
