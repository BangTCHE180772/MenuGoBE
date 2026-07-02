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

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
