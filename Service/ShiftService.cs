using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Shift;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;

namespace MenuGoBE.Service
{
    public class ShiftService : IShiftService
    {
        private readonly IShiftRepository _repo;
        private readonly IMapper _mapper;

        public ShiftService(IShiftRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<ShiftViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<ShiftViewDto>>(data);
        }

        public async Task<ShiftViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return null;
            return _mapper.Map<ShiftViewDto>(data);
        }

        public async Task<ShiftViewDto> CreateAsync(ShiftCreateDto dto)
        {
            var entity = _mapper.Map<Shift>(dto);
            entity.CreatedAt = DateTime.UtcNow;

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            // Re-fetch with includes for mapping
            var created = await _repo.GetByIdAsync(entity.Id);
            return _mapper.Map<ShiftViewDto>(created ?? entity);
        }

        public async Task<bool> UpdateAsync(ShiftUpdateDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            _mapper.Map(dto, entity);

            // Synchronize RoleRequirements
            entity.RoleRequirements.Clear();
            if (dto.RoleRequirements != null)
            {
                foreach (var req in dto.RoleRequirements)
                {
                    entity.RoleRequirements.Add(new ShiftRoleRequirement
                    {
                        ShiftId = entity.Id,
                        RoleId = req.RoleId,
                        MinQuantity = req.MinQuantity
                    });
                }
            }

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
