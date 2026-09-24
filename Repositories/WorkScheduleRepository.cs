using System.Collections.Generic;
using System.Linq;
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
            return await _context.WorkSchedules.AsNoTracking().ToListAsync();
        }

        public async Task<WorkSchedule?> GetByIdAsync(long id)
        {
            return await _context.WorkSchedules.FindAsync(id);
        }

        public async Task<WorkSchedule?> GetByIdWithDetailsAsync(long id)
        {
            return await _context.WorkSchedules
                .Include(ws => ws.Shift)
                .Include(ws => ws.Account)
                .AsNoTracking()
                .FirstOrDefaultAsync(ws => ws.Id == id);
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

        public async Task<bool> HasOverlappingScheduleAsync(long accountId, System.DateOnly workDate, TimeOnly startTime, TimeOnly endTime)
        {
            return await _context.WorkSchedules
                .Include(ws => ws.Shift)
                .AnyAsync(ws => ws.AccountId == accountId 
                             && ws.WorkDate == workDate
                             && ws.Shift.StartTime < endTime 
                             && ws.Shift.EndTime > startTime);
        }

        public async Task<List<WorkSchedule>> GetByAccountIdAsync(long accountId)
        {
            return await _context.WorkSchedules
                .AsNoTracking()
                .Where(ws => ws.AccountId == accountId)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<WorkSchedule>> GetTodaySchedulesAsync(long accountId, long branchId, DateOnly date)
        {
            var yesterday = date.AddDays(-1);
            return await _context.WorkSchedules
                .Include(ws => ws.Shift)
                .AsNoTracking()
                .Where(ws => ws.AccountId == accountId && ws.BranchId == branchId &&
                             (ws.WorkDate == date || 
                              (ws.WorkDate == yesterday && ws.Shift.StartTime > ws.Shift.EndTime)))
                .OrderBy(ws => ws.WorkDate).ThenBy(ws => ws.Shift.StartTime)
                .ToListAsync();
        }
        public async Task<List<WorkSchedule>> GetOpenSchedulesAsync()
        {
            return await _context.WorkSchedules
                .Include(ws => ws.Shift)
                .AsNoTracking()
                .Where(ws => ws.CheckInAt != null && ws.CheckOutAt == null)
                .ToListAsync();
        }

        public async Task<List<WorkSchedule>> GetPendingSchedulesByAccountIdsAsync(List<long> accountIds)
        {
            return await _context.WorkSchedules
                .Include(ws => ws.Shift)
                .AsNoTracking()
                .Where(ws => accountIds.Contains(ws.AccountId)
                          && ws.CheckInAt == null
                          && (ws.Status.ToUpper() == "APPROVED" || ws.Status.ToUpper() == "PENDING" || ws.Status.ToUpper() == "SCHEDULED"))
                .ToListAsync();
        }

        public async Task<List<WorkSchedule>> GetExpiredUncheckedInSchedulesAsync()
        {
            return await _context.WorkSchedules
                .Include(ws => ws.Shift)
                .AsNoTracking()
                .Where(ws => ws.CheckInAt == null
                          && ws.Status != "ABSENT"
                          && ws.Status != "Absent"
                          && ws.Status != "Completed"
                          && !ws.Status.StartsWith("LEAVE_APPROVED"))
                .ToListAsync();
        }

        public async Task<List<WorkSchedule>> GetTodaySchedulesByAccountAsync(long accountId, DateOnly date)
        {
            var yesterday = date.AddDays(-1);
            return await _context.WorkSchedules
                .Include(ws => ws.Shift)
                .AsNoTracking()
                .Where(ws => ws.AccountId == accountId && 
                             (ws.WorkDate == date || 
                              (ws.WorkDate == yesterday && ws.Shift.StartTime > ws.Shift.EndTime)))
                .OrderBy(ws => ws.WorkDate).ThenBy(ws => ws.Shift.StartTime)
                .ToListAsync();
        }
    }
}
