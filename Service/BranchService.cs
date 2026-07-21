using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Branch;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;

namespace MenuGoBE.Service
{
    public class BranchService : IBranchService
    {
        private readonly IBranchRepository _repo;
        private readonly IMapper _mapper;

        public BranchService(IBranchRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<BranchViewDto>> GetAllAsync()
        {
            var branches = await _repo.GetAllAsync();
            return _mapper.Map<List<BranchViewDto>>(branches);
        }

        public async Task<BranchViewDto?> GetByIdAsync(long id)
        {
            var branch = await _repo.GetByIdAsync(id);

            if (branch == null)
                return null;

            return _mapper.Map<BranchViewDto>(branch);
        }

        public async Task<List<BranchViewDto>> SearchFilteredBranchesAsync(BranchQueryDto dto)
        {
            var branches = await _repo.SearchFilteredBranchesAsync(dto);

            return _mapper.Map<List<BranchViewDto>>(branches);
        }

        public async Task<BranchViewDto> CreateAsync(BranchCreateDto dto)
        {
            var entity = _mapper.Map<Branch>(dto);

            entity.Address.Type = "Chi Nhánh";
            entity.Type = "Main";
            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            return _mapper.Map<BranchViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(BranchUpdateDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);

            if (entity == null)
                return false;

            _mapper.Map(dto, entity);

            entity.Address.Type = "Chi Nhánh";


            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);

            if (entity == null)
                return false;

            await _repo.DeleteAsync(id);
            await _repo.SaveChangesAsync();

            return true;
        }
    }
}