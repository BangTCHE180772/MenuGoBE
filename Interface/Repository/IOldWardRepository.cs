using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IOldWardRepository
    {
        Task<List<OldWard>> GetAllAsync();
    }
}
