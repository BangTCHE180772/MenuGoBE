using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface INewProvinceRepository
    {
        Task<List<NewProvince>> GetAllAsync();
    }
}
