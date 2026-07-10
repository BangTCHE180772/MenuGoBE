using AutoMapper;
using MenuGoBE.Dtos.Area;
using MenuGoBE.Dtos.Table;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;

namespace MenuGoBE.Service
{
    public class AreaService : IAreaService
    {
        private readonly IAreaRepository _repo;
        private readonly ITableRepository _tableRepo;
        private readonly IMapper _mapper;

        public AreaService(IAreaRepository repo, ITableRepository tableRepo, IMapper mapper)
        {
            _repo = repo;
            _tableRepo = tableRepo;
            _mapper = mapper;
        }

        public async Task<List<AreaViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<AreaViewDto>>(data);
        }

        public async Task<AreaViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return null;

            return _mapper.Map<AreaViewDto>(data);
        }

        public async Task<List<AreaViewDto>> GetByBranchIdAsync(long branchId)
        {
            var data = await _repo.GetByBranchIdAsync(branchId);
            return _mapper.Map<List<AreaViewDto>>(data);
        }

        public async Task<AreaViewDto> CreateAsync(AreaCreateDto dto)
        {
            var entity = _mapper.Map<Area>(dto);

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            return _mapper.Map<AreaViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(AreaUpdateDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            _mapper.Map(dto, entity);

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            await _repo.DeleteAsync(id);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<List<TableViewDto>> GetTablesByAreaIdAsync(long areaId)
        {
            var tables = await _tableRepo.GetAllAsync();
            var filtered = tables.Where(t => t.AreaId == areaId).ToList();
            return _mapper.Map<List<TableViewDto>>(filtered);
        }
    }
}
