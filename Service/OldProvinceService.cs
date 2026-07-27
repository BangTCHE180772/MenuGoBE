using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Address;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;

namespace MenuGoBE.Service
{
    public class OldProvinceService : IOldProvinceService
    {
        private readonly IOldProvinceRepository _repo;
        private readonly IMapper _mapper;

        public OldProvinceService(IOldProvinceRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<OldProvinceViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<OldProvinceViewDto>>(data);
        }
    }
}
