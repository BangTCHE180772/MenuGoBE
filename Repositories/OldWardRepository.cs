using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class OldWardRepository : IOldWardRepository
    {
        private readonly AppDbContext _context;

        public OldWardRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<OldWard>> GetAllAsync()
        {
            return await _context.OldWards
                .Include(w => w.OldDistrict)
                    .ThenInclude(d => d.OldProvince)
                .ToListAsync();
        }
    }
}
