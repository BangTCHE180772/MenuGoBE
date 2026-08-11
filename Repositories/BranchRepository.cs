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
            return await _context.Branches
                .Include(b => b.Address)
                    .ThenInclude(a => a.NewWard!)
                        .ThenInclude(w => w.NewProvince)
                .Include(b => b.Address)
                    .ThenInclude(a => a.OldWard!)
                        .ThenInclude(w => w.OldDistrict)
                            .ThenInclude(d => d.OldProvince)
                .Include(b => b.Contracts)
                    .ThenInclude(c => c.Account)
                .Include(b => b.Contracts)
                    .ThenInclude(c => c.Role)
                .ToListAsync();
        }

        public async Task<Branch?> GetByIdAsync(long id)
        {
            return await _context.Branches
                .Include(b => b.Address)
                .Include(b => b.Contracts)
                    .ThenInclude(c => c.Account)
                .Include(b => b.Contracts)
                    .ThenInclude(c => c.Role)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<List<Branch>> SearchFilteredBranchesAsync(BranchQueryDto dto)
        {
            var query = _context.Branches
                .Include(b => b.Address)
                    .ThenInclude(a => a.NewWard!)
                        .ThenInclude(w => w.NewProvince)
                .Include(b => b.Address)
                    .ThenInclude(a => a.OldWard!)
                        .ThenInclude(w => w.OldDistrict)
                            .ThenInclude(d => d.OldProvince)
                .Include(b => b.Contracts)
                    .ThenInclude(c => c.Account)
                .Include(b => b.Contracts)
                    .ThenInclude(c => c.Role)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(dto.Keyword))
            {
                query = query.Where(b =>
                    b.Name.ToLower().Contains(dto.Keyword.ToLower())
                    ||
                    b.Address.NewWard != null && b.Address.NewWard.Name.ToLower().Contains(dto.Keyword.ToLower())
                    ||
                    b.Address.NewWard!.NewProvince != null && b.Address.NewWard.NewProvince.Name.ToLower().Contains(dto.Keyword.ToLower())
                    );
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
                "id" => dto.Desc
                    ? query.OrderByDescending(b => b.Id)
                    : query.OrderBy(b => b.Id),

                "name" => dto.Desc
                    ? query.OrderByDescending(b => b.Name)
                    : query.OrderBy(b => b.Name),

                "createdat" => dto.Desc
                    ? query.OrderByDescending(b => b.CreatedAt)
                    : query.OrderBy(b => b.CreatedAt),

                "opentime" => dto.Desc
                    ? query.OrderByDescending(b => b.OpenTime)
                    : query.OrderBy(b => b.OpenTime),

                "closetime" => dto.Desc
                    ? query.OrderByDescending(b => b.CloseTime)
                    : query.OrderBy(b => b.CloseTime),

                "addressname" => dto.Desc
                    ? query.OrderByDescending(b => b.Address.NewWard!.Name)
                    : query.OrderBy(b => b.Address.NewWard!.Name),

                "managername" => dto.Desc
                    ? query.OrderByDescending(b => b.Contracts
                        .Where(c => c.Role.Name == "MANAGER" && c.Status == "Active")
                        .Select(c => c.Account.Name)
                        .FirstOrDefault())
                    : query.OrderBy(b => b.Contracts
                        .Where(c => c.Role.Name == "MANAGER" && c.Status == "Active")
                        .Select(c => c.Account.Name)
                        .FirstOrDefault()),

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