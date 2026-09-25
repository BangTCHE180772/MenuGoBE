using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class TableRepository : ITableRepository
    {
        private readonly AppDbContext _context;

        public TableRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Table>> GetAllAsync()
        {
            return await _context.Tables
                .Include(t => t.Area)
                .Where(t => t.IsActive)
                .ToListAsync();
        }

        public async Task<List<Table>> GetByBranchIdsAsync(List<long> branchIds)
        {
            return await _context.Tables
                .Include(t => t.Area)
                .Where(t => t.Area != null && branchIds.Contains(t.Area.BranchId) && t.IsActive)
                .ToListAsync();
        }

        public async Task<Table?> GetByIdAsync(long id)
        {
            var entity = await _context.Tables.Include(t => t.Area).FirstOrDefaultAsync(t => t.Id == id);
            if (entity != null && !entity.IsActive) return null;
            return entity;
        }

        public async Task<List<Table>> GetByIdsAsync(IEnumerable<long> ids)
        {
            return await _context.Tables.Include(t => t.Area)
                .Where(t => ids.Contains(t.Id) && t.IsActive)
                .ToListAsync();
        }

        public async Task<Table?> GetByNameAndAreaAsync(string name, long areaId)
        {
            return await _context.Tables
                .FirstOrDefaultAsync(t => t.AreaId == areaId && t.Name.ToLower() == name.ToLower());
        }

        public async Task CreateAsync(Table entity)
        {
            await _context.Tables.AddAsync(entity);
        }

        public Task UpdateAsync(Table entity)
        {
            _context.Tables.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.Tables.FindAsync(id);
            if (entity != null)
            {
                _context.Tables.Remove(entity);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
