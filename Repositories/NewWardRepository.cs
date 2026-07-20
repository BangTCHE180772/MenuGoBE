using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class NewWardRepository : INewWardRepository
    {
        private readonly AppDbContext _context;

        public NewWardRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<NewWard>> GetAllAsync()
        {
            return await _context.NewWards
                .Include(w => w.NewProvince)
                .ToListAsync();
        }
    }
}
