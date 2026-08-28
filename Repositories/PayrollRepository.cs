using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class PayrollRepository : IPayrollRepository
    {
        private readonly AppDbContext _context;

        public PayrollRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Payroll>> GetAllAsync(long? branchId = null, int? month = null, int? year = null, long? accountId = null, List<long>? roleIds = null)
        {
            var query = _context.Payrolls
                .Include(p => p.Account)
                    .ThenInclude(a => a.Contracts)
                .Include(p => p.Branch)
                .Include(p => p.Contract)
                .Include(p => p.Locker)
                .Include(p => p.Approver)
                .Include(p => p.SalaryDetails)
                .Include(p => p.PayrollShiftDetails)
                .AsQueryable();

            if (branchId.HasValue && branchId.Value > 0)
                query = query.Where(p => p.BranchId == branchId.Value);

            if (month.HasValue && month.Value > 0)
                query = query.Where(p => p.Month == month.Value);

            if (year.HasValue && year.Value > 0)
                query = query.Where(p => p.Year == year.Value);

            if (accountId.HasValue && accountId.Value > 0)
                query = query.Where(p => p.AccountId == accountId.Value);

            if (roleIds != null && roleIds.Count > 0)
            {
                query = query.Where(p => (p.Contract != null && roleIds.Contains(p.Contract.RoleId))
                                      || (p.Account != null && p.Account.Contracts.Any(c => roleIds.Contains(c.RoleId))));
            }

            return await query.OrderByDescending(p => p.Year)
                              .ThenByDescending(p => p.Month)
                              .ThenBy(p => p.AccountId)
                              .ToListAsync();
        }

        public async Task<Payroll?> GetByIdAsync(long id)
        {
            return await _context.Payrolls
                .Include(p => p.Account)
                .Include(p => p.Branch)
                .Include(p => p.Contract)
                .Include(p => p.Locker)
                .Include(p => p.Approver)
                .Include(p => p.SalaryDetails)
                    .ThenInclude(sd => sd.Account)
                .Include(p => p.PayrollShiftDetails)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Payroll>> GetByAccountIdAsync(long accountId, int? month = null, int? year = null)
        {
            var query = _context.Payrolls
                .Include(p => p.Account)
                .Include(p => p.Branch)
                .Include(p => p.Contract)
                .Include(p => p.Locker)
                .Include(p => p.Approver)
                .Include(p => p.SalaryDetails)
                    .ThenInclude(sd => sd.Account)
                .Include(p => p.PayrollShiftDetails)
                .Where(p => p.AccountId == accountId);

            if (month.HasValue && month.Value > 0)
                query = query.Where(p => p.Month == month.Value);

            if (year.HasValue && year.Value > 0)
                query = query.Where(p => p.Year == year.Value);

            return await query.OrderByDescending(p => p.Year)
                              .ThenByDescending(p => p.Month)
                              .ToListAsync();
        }

        public async Task<Payroll?> GetByAccountMonthYearAsync(long accountId, int month, int year)
        {
            return await _context.Payrolls
                .Include(p => p.Account)
                .Include(p => p.Branch)
                .Include(p => p.Contract)
                .Include(p => p.Locker)
                .Include(p => p.Approver)
                .Include(p => p.SalaryDetails)
                .Include(p => p.PayrollShiftDetails)
                .FirstOrDefaultAsync(p => p.AccountId == accountId && p.Month == month && p.Year == year);
        }

        public async Task CreateAsync(Payroll entity)
        {
            await _context.Payrolls.AddAsync(entity);
            await SaveChangesAsync();
        }

        public async Task UpdateAsync(Payroll entity)
        {
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                _context.Payrolls.Attach(entity);
                entry.State = EntityState.Modified;
            }
            await SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.Payrolls
                .Include(p => p.SalaryDetails)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entity != null)
            {
                if (entity.SalaryDetails != null && entity.SalaryDetails.Any())
                {
                    _context.SalaryDetails.RemoveRange(entity.SalaryDetails);
                }

                _context.Payrolls.Remove(entity);
                await SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
