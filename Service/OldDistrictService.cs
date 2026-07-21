using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Address;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;

namespace MenuGoBE.Service
{
    public class OldDistrictService : IOldDistrictService
    {
        private readonly IOldDistrictRepository _repo;
        private readonly IMapper _mapper;

        public OldDistrictService(IOldDistrictRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<OldDistrictViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<OldDistrictViewDto>>(data);
        }
    }
}
