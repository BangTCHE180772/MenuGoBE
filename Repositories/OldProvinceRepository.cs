using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class OldProvinceRepository : IOldProvinceRepository
    {
        private readonly AppDbContext _context;

        public OldProvinceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<OldProvince>> GetAllAsync()
        {
            return await _context.OldProvinces.ToListAsync();
        }
    }
}
