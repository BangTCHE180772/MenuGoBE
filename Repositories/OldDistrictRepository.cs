using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class OldDistrictRepository : IOldDistrictRepository
    {
        private readonly AppDbContext _context;

        public OldDistrictRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<OldDistrict>> GetAllAsync()
        {
            return await _context.OldDistricts
                .Include(d => d.OldProvince)
                .ToListAsync();
        }
    }
}
