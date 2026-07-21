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
    }
}
