using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Address;

namespace MenuGoBE.Interface.Services
{
    public interface INewWardService
    {
        Task<List<NewWardViewDto>> GetAllAsync();
    }
}
