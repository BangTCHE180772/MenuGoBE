using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class GroupRepository : IGroupRepository
    {
        private readonly AppDbContext _context;

        public GroupRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Group>> GetAllAsync()
        {
            return await _context.Groups.ToListAsync();
        }

        public async Task<Group?> GetByIdAsync(long id)
        {
            return await _context.Groups.FindAsync(id);
        }

        public async Task CreateAsync(Group entity)
        {
            await _context.Groups.AddAsync(entity);
        }

        public Task UpdateAsync(Group entity)
        {
            _context.Groups.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.Groups.FindAsync(id);
            if (entity != null)
                _context.Groups.Remove(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
