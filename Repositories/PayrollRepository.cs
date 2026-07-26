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

        public async Task<List<Payroll>> GetAllAsync(long? branchId = null, int? month = null, int? year = null, long? accountId = null)
        {
            var query = _context.Payrolls
                .Include(p => p.Account)
                .Include(p => p.Branch)
                .Include(p => p.Contract)
                .Include(p => p.SalaryDetails)
                .AsQueryable();

            if (branchId.HasValue && branchId.Value > 0)
                query = query.Where(p => p.BranchId == branchId.Value);

            if (month.HasValue && month.Value > 0)
                query = query.Where(p => p.Month == month.Value);

            if (year.HasValue && year.Value > 0)
                query = query.Where(p => p.Year == year.Value);

            if (accountId.HasValue && accountId.Value > 0)
                query = query.Where(p => p.AccountId == accountId.Value);

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
                .Include(p => p.SalaryDetails)
                    .ThenInclude(sd => sd.Account)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Payroll?> GetByAccountMonthYearAsync(long accountId, int month, int year)
        {
            return await _context.Payrolls
                .Include(p => p.Account)
                .Include(p => p.Branch)
                .Include(p => p.Contract)
                .Include(p => p.SalaryDetails)
                .FirstOrDefaultAsync(p => p.AccountId == accountId && p.Month == month && p.Year == year);
        }

        public async Task CreateAsync(Payroll entity)
        {
            await _context.Payrolls.AddAsync(entity);
            await SaveChangesAsync();
        }

        public async Task UpdateAsync(Payroll entity)
        {
            _context.Payrolls.Update(entity);
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
