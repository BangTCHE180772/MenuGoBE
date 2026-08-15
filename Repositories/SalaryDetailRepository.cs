using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class SalaryDetailRepository : ISalaryDetailRepository
    {
        private readonly AppDbContext _context;

        public SalaryDetailRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SalaryDetail>> GetAllAsync(long? payrollId = null, long? accountId = null, List<long>? branchIds = null, List<long>? roleIds = null)
        {
            var query = _context.SalaryDetails
                .Include(sd => sd.Account)
                    .ThenInclude(a => a.Contracts)
                .Include(sd => sd.Creator)
                .Include(sd => sd.Payroll)
                    .ThenInclude(p => p.Contract)
                .AsQueryable();

            if (payrollId.HasValue && payrollId.Value > 0)
            {
                query = query.Where(sd => sd.PayrollId == payrollId.Value);
            }

            if (accountId.HasValue && accountId.Value > 0)
            {
                query = query.Where(sd => sd.AccountId == accountId.Value);
            }

            if (branchIds != null && branchIds.Count > 0)
            {
                query = query.Where(sd => sd.Payroll != null && branchIds.Contains(sd.Payroll.BranchId));
            }

            if (roleIds != null && roleIds.Count > 0)
            {
                query = query.Where(sd => (sd.Payroll != null && sd.Payroll.Contract != null && roleIds.Contains(sd.Payroll.Contract.RoleId))
                                       || (sd.Account != null && sd.Account.Contracts.Any(c => roleIds.Contains(c.RoleId))));
            }

            return await query.OrderByDescending(sd => sd.CreatedAt).ToListAsync();
        }

        public async Task<SalaryDetail?> GetByIdAsync(long id)
        {
            return await _context.SalaryDetails
                .Include(sd => sd.Account)
                .Include(sd => sd.Creator)
                .Include(sd => sd.Payroll)
                .FirstOrDefaultAsync(sd => sd.Id == id);
        }

        public async Task CreateAsync(SalaryDetail entity)
        {
            await _context.SalaryDetails.AddAsync(entity);
            await SaveChangesAsync();
        }

        public async Task UpdateAsync(SalaryDetail entity)
        {
            _context.SalaryDetails.Update(entity);
            await SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.SalaryDetails.FindAsync(id);
            if (entity != null)
            {
                _context.SalaryDetails.Remove(entity);
                await SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
