using MenuGoBE.Data;
using MenuGoBE.Dtos.Payroll;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories;

public class PayrollSuggestionRepository : IPayrollSuggestionRepository
{
    private readonly AppDbContext _context;

    public PayrollSuggestionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PayrollSuggestion?> GetByIdAsync(long id)
    {
        return await _context.PayrollSuggestions
            .Include(s => s.Branch)
            .Include(s => s.Account)
            .Include(s => s.Creator)
            .Include(s => s.Processor)
            .Include(s => s.AppliedPayroll)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<PayrollSuggestion>> GetFilteredAsync(PayrollSuggestionQueryDto query)
    {
        var q = _context.PayrollSuggestions
            .Include(s => s.Branch)
            .Include(s => s.Account)
            .Include(s => s.Creator)
            .Include(s => s.Processor)
            .AsQueryable();

        if (query.BranchId.HasValue && query.BranchId.Value > 0)
        {
            q = q.Where(s => s.BranchId == query.BranchId.Value);
        }

        if (query.AccountId.HasValue && query.AccountId.Value > 0)
        {
            q = q.Where(s => s.AccountId == query.AccountId.Value);
        }

        if (query.Month.HasValue && query.Month.Value > 0)
        {
            q = q.Where(s => s.Month == query.Month.Value);
        }

        if (query.Year.HasValue && query.Year.Value > 0)
        {
            q = q.Where(s => s.Year == query.Year.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<PayrollSuggestionStatus>(query.Status, true, out var parsedStatus))
        {
            q = q.Where(s => s.Status == parsedStatus);
        }

        return await q.OrderByDescending(s => s.CreatedAt).ToListAsync();
    }

    public async Task<List<PayrollSuggestion>> GetApprovedUnappliedForMonthAsync(long branchId, long accountId, int month, int year)
    {
        return await _context.PayrollSuggestions
            .Where(s => s.BranchId == branchId &&
                        s.AccountId == accountId &&
                        s.Status == PayrollSuggestionStatus.Approved &&
                        !s.IsApplied &&
                        s.Month == month &&
                        s.Year == year)
            .ToListAsync();
    }

    public async Task CreateAsync(PayrollSuggestion entity)
    {
        await _context.PayrollSuggestions.AddAsync(entity);
    }

    public Task UpdateAsync(PayrollSuggestion entity)
    {
        _context.PayrollSuggestions.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PayrollSuggestion entity)
    {
        _context.PayrollSuggestions.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
