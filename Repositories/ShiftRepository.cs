using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class ShiftRepository : IShiftRepository
    {
        private readonly AppDbContext _context;

        public ShiftRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Shift>> GetAllAsync()
        {
            return await _context.Shifts
                .Include(s => s.RoleRequirements)
                .ThenInclude(rr => rr.Role)
                .ToListAsync();
        }

        public async Task<Shift?> GetByIdAsync(long id)
        {
            return await _context.Shifts
                .Include(s => s.RoleRequirements)
                .ThenInclude(rr => rr.Role)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Shift?> GetByTimeAsync(TimeOnly startTime, TimeOnly endTime)
        {
            return await _context.Shifts
                .Include(s => s.RoleRequirements)
                .ThenInclude(rr => rr.Role)
                .FirstOrDefaultAsync(s => s.StartTime == startTime && s.EndTime == endTime);
        }

        public async Task CreateAsync(Shift entity)
        {
            await _context.Shifts.AddAsync(entity);
        }

        public Task UpdateAsync(Shift entity)
        {
            var tracked = _context.Shifts.Local.FirstOrDefault(e => e.Id == entity.Id);
            if (tracked != null)
            {
                _context.Entry(tracked).CurrentValues.SetValues(entity);
            }
            else
            {
                _context.Shifts.Update(entity);
            }
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.Shifts.FindAsync(id);
            if (entity != null)
            {
                _context.Shifts.Remove(entity);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
