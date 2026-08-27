using AutoMapper;
using MenuGoBE.Dtos.KitchenStation;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Exceptions;

namespace MenuGoBE.Service
{
    public class KitchenStationService : IKitchenStationService
    {
        private readonly IKitchenStationRepository _repo;
        private readonly IMapper _mapper;

        public KitchenStationService(IKitchenStationRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<KitchenStationViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<KitchenStationViewDto>>(data);
        }

        public async Task<KitchenStationViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return null;

            return _mapper.Map<KitchenStationViewDto>(data);
        }

        public async Task<List<KitchenStationViewDto>> GetByBranchIdAsync(long branchId)
        {
            var data = await _repo.GetByBranchIdAsync(branchId);
            return _mapper.Map<List<KitchenStationViewDto>>(data);
        }

        public async Task<KitchenStationViewDto> CreateAsync(KitchenStationCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new MenuGoException(new ErrorResult(400, "Tên trạm bếp không được để trống.", 400));
            }

            dto.Name = dto.Name.Trim();

            var existing = await _repo.GetByNameAndBranchAsync(dto.Name, dto.BranchId);
            if (existing != null)
            {
                if (existing.IsActive)
                {
                    throw new MenuGoException(new ErrorResult(400, "Đã tồn tại trạm bếp với tên này trong chi nhánh.", 400));
                }

                _mapper.Map(dto, existing);
                existing.IsActive = true;
                await _repo.UpdateAsync(existing);
                await _repo.SaveChangesAsync();
                return _mapper.Map<KitchenStationViewDto>(existing);
            }

            var entity = _mapper.Map<KitchenStation>(dto);

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            return _mapper.Map<KitchenStationViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(KitchenStationUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new MenuGoException(new ErrorResult(400, "Tên trạm bếp không được để trống.", 400));
            }

            dto.Name = dto.Name.Trim();

            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            if (dto.Name.ToLower() != entity.Name.ToLower())
            {
                var existing = await _repo.GetByNameAndBranchAsync(dto.Name, entity.BranchId);
                if (existing != null && existing.IsActive)
                {
                    throw new MenuGoException(new ErrorResult(400, "Đã tồn tại trạm bếp với tên này trong chi nhánh.", 400));
                }
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

            var isUsed = await _repo.IsUsedByAnyProductAsync(id);
            if (isUsed)
            {
                throw new MenuGoException(new ErrorResult(400, "Không thể xóa trạm bếp này vì đang có món ăn được gán vào.", 400));
            }

            entity.IsActive = false;
            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            return true;
        }
    }
}
