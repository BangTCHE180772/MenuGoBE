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
    public class ChainRepository : IChainRepository
    {
        private readonly AppDbContext _context;

        public ChainRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Chain>> GetAllAsync()
        {
            return await _context.Chains
            .Include(b => b.Address)
                .ThenInclude(a => a.NewWard!)
                    .ThenInclude(w => w.NewProvince)
            .Include(b => b.Address)
                .ThenInclude(a => a.OldWard!)
                    .ThenInclude(w => w.OldDistrict)
                        .ThenInclude(d => d.OldProvince)
            .ToListAsync();
        }

        public async Task<Chain?> GetMainChainAsync()
        {
            return await _context.Chains
            .Include(b => b.Address)
                .ThenInclude(a => a.NewWard!)
                    .ThenInclude(w => w.NewProvince)
            .Include(b => b.Address)
                .ThenInclude(a => a.OldWard!)
                    .ThenInclude(w => w.OldDistrict)
                        .ThenInclude(d => d.OldProvince)
            .FirstOrDefaultAsync();
        }

        public async Task<Chain?> GetByIdAsync(long id)
        {
            return await _context.Chains
            .Include(b => b.Address)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task CreateAsync(Chain entity)
        {
            await _context.Chains.AddAsync(entity);
        }

        public Task UpdateAsync(Chain entity)
        {
            _context.Chains.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.Chains.FindAsync(id);
            if (entity != null)
            {
                _context.Chains.Remove(entity);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}