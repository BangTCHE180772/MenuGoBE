using AutoMapper;
using MenuGoBE.Dtos.Group;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;

namespace MenuGoBE.Service
{
    public class GroupService : IGroupService
    {
        private readonly IGroupRepository _repo;
        private readonly IMapper _mapper;

        public GroupService(IGroupRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<GroupViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<GroupViewDto>>(data);
        }

        public async Task<List<GroupViewDto>> GetSellableAsync(long? branchId = null)
        {
            var data = await _repo.GetSellableAsync(branchId);
            return _mapper.Map<List<GroupViewDto>>(data);
        }

        public async Task<GroupViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return null;
            return _mapper.Map<GroupViewDto>(data);
        }

        public async Task<GroupViewDto> CreateAsync(GroupCreateDto dto)
        {
            if (await _repo.ExistsByNameAsync(dto.Name))
            {
                throw new MenuGoException(ErrorCodes.DuplicateGroupName);
            }

            var entity = _mapper.Map<Group>(dto);
            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();
            return _mapper.Map<GroupViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(GroupUpdateDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            if (await _repo.ExistsByNameExcludeIdAsync(dto.Name, dto.Id))
            {
                throw new MenuGoException(ErrorCodes.DuplicateGroupName);
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

            await _repo.DeleteAsync(id);
            await _repo.SaveChangesAsync();
            return true;
        }
    }
}
