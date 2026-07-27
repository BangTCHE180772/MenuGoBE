using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Chain;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;

namespace MenuGoBE.Service
{
    public class ChainService : IChainService
    {
        private readonly IChainRepository _repo;
        private readonly IMapper _mapper;

        public ChainService(IChainRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<ChainViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<ChainViewDto>>(data);
        }

        public async Task<ChainViewDto?> GetMainChainAsync()
        {
            var chain = await _repo.GetMainChainAsync();
            return _mapper.Map<ChainViewDto>(chain);
        }

        public async Task<ChainViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return null;

            return _mapper.Map<ChainViewDto>(data);
        }

        public async Task<ChainViewDto> CreateAsync(ChainCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new MenuGoException(ErrorCodes.ChainNameRequired);
            }

            if (dto.Name.Length > 100)
            {
                throw new MenuGoException(ErrorCodes.ChainNameLengthExceeded);
            }

            if (dto.OpenTime >= dto.CloseTime)
            {
                throw new MenuGoException(ErrorCodes.ChainOpenCloseTimeInvalid);
            }

            if (!string.IsNullOrEmpty(dto.LogoImage) && dto.LogoImage.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.ChainImageLengthExceeded);
            }

            if (!string.IsNullOrEmpty(dto.BackgroundImage) && dto.BackgroundImage.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.ChainImageLengthExceeded);
            }

            var entity = _mapper.Map<Chain>(dto);

            entity.Address.Type = "Chuỗi";

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            return _mapper.Map<ChainViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(ChainUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new MenuGoException(ErrorCodes.ChainNameRequired);
            }

            if (dto.Name.Length > 100)
            {
                throw new MenuGoException(ErrorCodes.ChainNameLengthExceeded);
            }

            if (dto.OpenTime >= dto.CloseTime)
            {
                throw new MenuGoException(ErrorCodes.ChainOpenCloseTimeInvalid);
            }

            if (!string.IsNullOrEmpty(dto.LogoImage) && dto.LogoImage.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.ChainImageLengthExceeded);
            }

            if (!string.IsNullOrEmpty(dto.BackgroundImage) && dto.BackgroundImage.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.ChainImageLengthExceeded);
            }

            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            // Lưu giữ WardId cũ trước khi map
            var oldNewWardId = entity.Address?.NewWardId;
            var oldOldWardId = entity.Address?.OldWardId;

            _mapper.Map(dto, entity);

            if (entity.Address != null)
            {
                entity.Address.Type = "Chuỗi";
                // Nếu dto truyền null (không chọn địa chỉ mới), giữ nguyên WardId cũ trong DB
                if (!dto.Address.NewWardId.HasValue)
                {
                    entity.Address.NewWardId = oldNewWardId;
                }
                if (!dto.Address.OldWardId.HasValue)
                {
                    entity.Address.OldWardId = oldOldWardId;
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