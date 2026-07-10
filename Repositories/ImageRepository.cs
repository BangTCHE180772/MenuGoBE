using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class ImageRepository : IImageRepository
    {
        private readonly AppDbContext _context;

        public ImageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Models.Image>> GetAllAsync()
        {
            return await _context.Images.ToListAsync();
        }

        public async Task<Models.Image?> GetByIdAsync(long id)
        {
            return await _context.Images.FindAsync(id);
        }

        public async Task CreateAsync(Models.Image entity)
        {
            await _context.Images.AddAsync(entity);
        }

        public Task UpdateAsync(Models.Image entity)
        {
            _context.Images.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.Images.FindAsync(id);
            if (entity != null)
                _context.Images.Remove(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
