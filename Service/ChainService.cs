using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Chain;
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
            var entity = _mapper.Map<Chain>(dto);

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            return _mapper.Map<ChainViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(ChainUpdateDto dto)
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
    }
}