using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Contract;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;

namespace MenuGoBE.Service
{
    public class ContractService : IContractService
    {
        private readonly IContractRepository _repo;
        private readonly IMapper _mapper;

        public ContractService(IContractRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<ContractViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<ContractViewDto>>(data);
        }

        public async Task<ContractViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return null;
            return _mapper.Map<ContractViewDto>(data);
        }

        public async Task<ContractViewDto> CreateAsync(ContractCreateDto dto)
        {
            if (dto.Status == "Active" && await _repo.IsManagerRoleAsync(dto.RoleId))
            {
                if (await _repo.HasActiveManagerAsync(dto.BranchId))
                {
                    throw new Exception("Chi nhánh này đã có Quản lý. Mỗi chi nhánh chỉ được phép có duy nhất 1 Quản lý.");
                }
            }

            var entity = _mapper.Map<Contract>(dto);
            entity.CreatedAt = DateTime.UtcNow;

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            return _mapper.Map<ContractViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(ContractUpdateDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            if (dto.Status == "Active" && await _repo.IsManagerRoleAsync(dto.RoleId))
            {
                if (await _repo.HasActiveManagerAsync(dto.BranchId, dto.Id))
                {
                    throw new Exception("Chi nhánh này đã có Quản lý. Mỗi chi nhánh chỉ được phép có duy nhất 1 Quản lý.");
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

            await _repo.DeleteAsync(id);
            await _repo.SaveChangesAsync();

            return true;
        }
    }
}
