using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class WorkScheduleRepository : IWorkScheduleRepository
    {
        private readonly AppDbContext _context;

        public WorkScheduleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<WorkSchedule>> GetAllAsync()
        {
            return await _context.WorkSchedules.ToListAsync();
        }

        public async Task<WorkSchedule?> GetByIdAsync(long id)
        {
            return await _context.WorkSchedules.FindAsync(id);
        }

        public async Task CreateAsync(WorkSchedule entity)
        {
            await _context.WorkSchedules.AddAsync(entity);
        }

        public Task UpdateAsync(WorkSchedule entity)
        {
            _context.WorkSchedules.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.WorkSchedules.FindAsync(id);
            if (entity != null)
            {
                _context.WorkSchedules.Remove(entity);
            }
        }

        public async Task<bool> ExistsAsync(long accountId, long shiftId, System.DateOnly workDate)
        {
            return await _context.WorkSchedules.AnyAsync(ws => ws.AccountId == accountId && ws.ShiftId == shiftId && ws.WorkDate == workDate);
        }

        public async Task<bool> BranchExistsAsync(long branchId)
        {
            return await _context.Branches.AnyAsync(b => b.Id == branchId);
        }

        public async Task<bool> ShiftExistsAsync(long shiftId)
        {
            return await _context.Shifts.AnyAsync(s => s.Id == shiftId);
        }

        public async Task<List<long>> GetValidAccountIdsAsync(List<long> accountIds)
        {
            if (accountIds == null || accountIds.Count == 0) return new List<long>();
            return await _context.Accounts
                .Where(a => accountIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<WorkSchedule>> GetTodaySchedulesAsync(long accountId, long branchId, DateOnly date)
        {
            return await _context.WorkSchedules
                .Include(ws => ws.Shift)
                .Where(ws => ws.AccountId == accountId 
                          && ws.BranchId == branchId 
                          && ws.WorkDate == date)
                .OrderBy(ws => ws.Shift.StartTime)
                .ToListAsync();
        }
    }
}
