using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Branch;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class BranchRepository : IBranchRepository
    {
        private readonly AppDbContext _context;

        public BranchRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Branch>> GetAllAsync()
        {
            return await _context.Branches.ToListAsync();
        }

        public async Task<Branch?> GetByIdAsync(long id)
        {
            return await _context.Branches.FindAsync(id);
        }

        public async Task<List<Branch>> SearchFilteredBranchesAsync(BranchQueryDto dto)
        {
            var query = _context.Branches
                .Include(b => b.Address)
                    .ThenInclude(a => a.NewWard!)
                        .ThenInclude(w => w.NewProvince)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(dto.Keyword))
            {
                query = query.Where(b =>
                    b.Name.ToLower().Contains(dto.Keyword.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(dto.Type))
            {
                query = query.Where(b => b.Type.ToLower() == dto.Type.ToLower());
            }

            if (dto.NewProvinceId.HasValue)
            {
                query = query.Where(b =>
                    b.Address.NewWard != null &&
                    b.Address.NewWard.NewProvinceId == dto.NewProvinceId);
            }

            query = dto.SortBy?.ToLowerInvariant() switch
            {
                "Name" => dto.Desc
                    ? query.OrderByDescending(b => b.Name)
                    : query.OrderBy(b => b.Name),

                "CreatedAt" => dto.Desc
                    ? query.OrderByDescending(b => b.CreatedAt)
                    : query.OrderBy(b => b.CreatedAt),

                _ => query.OrderBy(b => b.Id)
            };

            query = query.Skip((dto.Page - 1) * dto.PageSize)
                         .Take(dto.PageSize);

            return await query.ToListAsync();
        }

        public async Task CreateAsync(Branch branch)
        {
            await _context.Branches.AddAsync(branch);
        }

        public Task UpdateAsync(Branch branch)
        {
            _context.Branches.Update(branch);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await GetByIdAsync(id);

            if (entity != null)
                _context.Branches.Remove(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}