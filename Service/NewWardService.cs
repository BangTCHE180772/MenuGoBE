using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Address;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;

namespace MenuGoBE.Service
{
    public class NewWardService : INewWardService
    {
        private readonly INewWardRepository _repo;
        private readonly IMapper _mapper;

        public NewWardService(INewWardRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<NewWardViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<NewWardViewDto>>(data);
        }
    }
}
