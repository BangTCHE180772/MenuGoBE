using AutoMapper;
using MenuGoBE.Dtos.Unit;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;

namespace MenuGoBE.Service
{
    public class UnitService : IUnitService
    {
        private readonly IUnitRepository _repo;
        private readonly IMapper _mapper;

        public UnitService(IUnitRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<UnitViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<UnitViewDto>>(data);
        }

        public async Task<UnitViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return null;
            return _mapper.Map<UnitViewDto>(data);
        }

        public async Task<UnitViewDto> CreateAsync(UnitCreateDto dto)
        {
            if (await _repo.ExistsByNameAsync(dto.Name))
            {
                throw new MenuGoException(ErrorCodes.DuplicateUnitName);
            }

            var entity = _mapper.Map<Unit>(dto);
            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();
            return _mapper.Map<UnitViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(UnitUpdateDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            if (await _repo.ExistsByNameExcludeIdAsync(dto.Name, dto.Id))
            {
                throw new MenuGoException(ErrorCodes.DuplicateUnitName);
            }

            _mapper.Map(dto, entity);
            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            if (await _repo.HasConversionsAsync(id))
            {
                throw new MenuGoException(ErrorCodes.UnitInUse);
            }

            await _repo.DeleteAsync(id);
            await _repo.SaveChangesAsync();
            return true;
        }
    }
}
