using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class ShiftChangeRequestRepository : IShiftChangeRequestRepository
    {
        private readonly AppDbContext _context;

        public ShiftChangeRequestRepository(AppDbContext context)
        {
            _context = context;
        }

        private IQueryable<ShiftChangeRequest> BaseQuery()
        {
            return _context.ShiftChangeRequests
                .Include(r => r.Account)
                .Include(r => r.OldShift)
                .Include(r => r.NewShift)
                .Include(r => r.Approver);
        }

        public async Task<ShiftChangeRequest?> GetByIdAsync(long id)
        {
            return await BaseQuery().FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<ShiftChangeRequest>> GetByBranchAsync(long branchId, ShiftChangeRequestStatus? status)
        {
            var query = BaseQuery().Where(r => r.BranchId == branchId);
            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            return await query.OrderByDescending(r => r.RequestedAt).ToListAsync();
        }

        public async Task<List<ShiftChangeRequest>> GetByAccountIdAsync(long accountId)
        {
            return await BaseQuery()
                .Where(r => r.AccountId == accountId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<bool> HasPendingForScheduleAsync(long workScheduleId)
        {
            return await _context.ShiftChangeRequests
                .AnyAsync(r => r.WorkScheduleId == workScheduleId && r.Status == ShiftChangeRequestStatus.Pending);
        }

        public async Task CreateAsync(ShiftChangeRequest entity)
        {
            await _context.ShiftChangeRequests.AddAsync(entity);
        }

        public Task UpdateAsync(ShiftChangeRequest entity)
        {
            _context.ShiftChangeRequests.Update(entity);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
