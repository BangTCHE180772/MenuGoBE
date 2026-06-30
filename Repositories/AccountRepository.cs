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

        public async Task<List<Account>> GetAccountsWithRoleRangeAsync(int minRole, int maxRole)
        {
            return await _context.Accounts
                .Include(a => a.TempRoles)
                .Where(a => a.TempRoles.Any(tr => tr.RoleId >= minRole && tr.RoleId <= maxRole))
                .ToListAsync();
        }
    }
}
