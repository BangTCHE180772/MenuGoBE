using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IOldProvinceRepository
    {
        Task<List<OldProvince>> GetAllAsync();
    }
}
