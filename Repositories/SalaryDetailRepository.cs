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

        public async Task<List<SalaryDetail>> GetAllAsync(long? payrollId = null, long? accountId = null)
        {
            var query = _context.SalaryDetails
                .Include(sd => sd.Account)
                .Include(sd => sd.Payroll)
                .AsQueryable();

            if (payrollId.HasValue && payrollId.Value > 0)
            {
                query = query.Where(sd => sd.PayrollId == payrollId.Value);
            }

            if (accountId.HasValue && accountId.Value > 0)
            {
                query = query.Where(sd => sd.AccountId == accountId.Value);
            }

            return await query.OrderByDescending(sd => sd.CreatedAt).ToListAsync();
        }

        public async Task<SalaryDetail?> GetByIdAsync(long id)
        {
            return await _context.SalaryDetails
                .Include(sd => sd.Account)
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
