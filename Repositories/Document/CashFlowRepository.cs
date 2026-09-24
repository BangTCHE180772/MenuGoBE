using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Repositories.Document
{
    public class CashFlowRepository : ICashFlowRepository
    {
        private readonly AppDbContext _context;

        public CashFlowRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<CashFlow>> GetByBranchIdAsync(long? branchId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.CashFlows
                .AsNoTracking()
                .Where(c => !c.IsDeleted);

            if (branchId.HasValue && branchId.Value > 0)
            {
                query = query.Where(c => c.BranchId == branchId.Value);
            }

            return await query
                .Include(c => c.Partner)
                .Include(c => c.Document)
                .OrderByDescending(c => c.BusinessDate)
                .ThenByDescending(c => c.PostingSequence)
                .ToListAsync();
        }
        public async Task<CashFlow?> GetByIdAsync(long id)
        {
            return await _context.CashFlows
                .Where(c => c.Id == id && !c.IsDeleted)
                .Include(c => c.Partner)
                .Include(c => c.Document)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(CashFlow cashFlow)
        {
            await _context.CashFlows.AddAsync(cashFlow);
        }

        public void Update(CashFlow cashFlow)
        {
            _context.CashFlows.Update(cashFlow);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
