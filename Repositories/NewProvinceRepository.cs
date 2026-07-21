using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class NewProvinceRepository : INewProvinceRepository
    {
        private readonly AppDbContext _context;

        public NewProvinceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<NewProvince>> GetAllAsync()
        {
            return await _context.NewProvinces.ToListAsync();
        }
    }
}
