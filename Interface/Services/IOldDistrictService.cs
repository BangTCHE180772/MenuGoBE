using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Address;

namespace MenuGoBE.Interface.Services
{
    public interface IOldDistrictService
    {
        Task<List<OldDistrictViewDto>> GetAllAsync();
    }
}
