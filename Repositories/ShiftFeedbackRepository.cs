using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories;

public class ShiftFeedbackRepository : IShiftFeedbackRepository
{
    private readonly AppDbContext _context;

    public ShiftFeedbackRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<WorkScheduleFeedback> BaseQuery()
    {
        return _context.WorkScheduleFeedbacks
            .Include(f => f.Account)
            .Include(f => f.Branch)
            .Include(f => f.WorkSchedule)
                .ThenInclude(ws => ws.Shift)
            .Include(f => f.Resolver)
            .AsNoTracking();
    }

    public async Task<WorkScheduleFeedback?> GetByIdAsync(long id)
    {
        return await BaseQuery().FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<List<WorkScheduleFeedback>> GetByAccountIdAsync(long accountId)
    {
        return await BaseQuery()
            .Where(f => f.AccountId == accountId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<WorkScheduleFeedback>> GetByBranchAsync(long branchId, ShiftFeedbackStatus? status)
    {
        var query = BaseQuery().Where(f => f.BranchId == branchId);
        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        return await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
    }

    public async Task CreateAsync(WorkScheduleFeedback entity)
    {
        await _context.WorkScheduleFeedbacks.AddAsync(entity);
    }

    public Task UpdateAsync(WorkScheduleFeedback entity)
    {
        _context.WorkScheduleFeedbacks.Update(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
