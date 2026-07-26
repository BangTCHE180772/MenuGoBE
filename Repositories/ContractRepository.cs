using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class ContractRepository : IContractRepository
    {
        private readonly AppDbContext _context;

        public ContractRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Contract>> GetAllAsync()
        {
            return await _context.Contracts
                .Include(c => c.Account)
                .Include(c => c.Role)
                .Include(c => c.Branch)
                .ToListAsync();
        }

        public async Task<List<Contract>> GetByBranchIdsAsync(List<long> branchIds)
        {
            return await _context.Contracts
                .Include(c => c.Account)
                .Include(c => c.Role)
                .Include(c => c.Branch)
                .Where(c => branchIds.Contains(c.BranchId))
                .ToListAsync();
        }

        public async Task<List<Contract>> GetContractsByRolesAsync(List<long> roleIds, long? branchId = null, List<long>? branchIds = null)
        {
            var query = _context.Contracts
                .Include(c => c.Account)
                .Include(c => c.Role)
                .Include(c => c.Branch)
                .Where(c => roleIds.Contains(c.RoleId));

            if (branchId.HasValue)
            {
                query = query.Where(c => c.BranchId == branchId.Value);
            }
            else if (branchIds != null && branchIds.Count > 0)
            {
                query = query.Where(c => branchIds.Contains(c.BranchId));
            }

            return await query.ToListAsync();
        }

        public async Task<Contract?> GetByIdAsync(long id)
        {
            return await _context.Contracts
                .Include(c => c.Account)
                .Include(c => c.Role)
                .Include(c => c.Branch)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task CreateAsync(Contract entity)
        {
            await _context.Contracts.AddAsync(entity);
        }

        public Task UpdateAsync(Contract entity)
        {
            _context.Contracts.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.Contracts.FindAsync(id);
            if (entity != null)
            {
                _context.Contracts.Remove(entity);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasActiveManagerAsync(long branchId, long excludeContractId = 0)
        {
            return await _context.Contracts
                .Include(c => c.Role)
                .AnyAsync(c => c.BranchId == branchId 
                            && c.Role.Name == "Manager" 
                            && c.Status == "Active"
                            && c.Id != excludeContractId);
        }

        public async Task<bool> IsManagerRoleAsync(long roleId)
        {
            var role = await _context.Roles.FindAsync(roleId);
            return role != null && role.Name == "Manager";
        }
    }
}
