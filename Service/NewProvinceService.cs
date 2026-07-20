using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Address;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;

namespace MenuGoBE.Service
{
    public class NewProvinceService : INewProvinceService
    {
        private readonly INewProvinceRepository _repo;
        private readonly IMapper _mapper;

        public NewProvinceService(INewProvinceRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<NewProvinceViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<NewProvinceViewDto>>(data);
        }
    }
}
