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

        public async Task<List<ContractViewDto>> GetByBranchIdsAsync(List<long> branchIds)
        {
            var data = await _repo.GetByBranchIdsAsync(branchIds);
            return _mapper.Map<List<ContractViewDto>>(data);
        }

        public async Task<List<ContractViewDto>> GetContractsByRolesAsync(List<long> roleIds, long? branchId = null, List<long>? branchIds = null)
        {
            var data = await _repo.GetContractsByRolesAsync(roleIds, branchId, branchIds);
            return _mapper.Map<List<ContractViewDto>>(data);
        }

        public async Task<ContractViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return null;
            return _mapper.Map<ContractViewDto>(data);
        }

        public async Task<List<ContractViewDto>> GetByAccountIdAsync(long accountId)
        {
            var data = await _repo.GetByAccountIdAsync(accountId);
            return _mapper.Map<List<ContractViewDto>>(data);
        }

        public async Task<ContractViewDto> CreateAsync(ContractCreateDto dto)
        {
            if (dto.Status == "Expired")
            {
                throw new Exception("Không thể chọn trạng thái 'Hết hiệu lực' khi tạo hợp đồng. Trạng thái này do hệ thống tự động cập nhật khi hợp đồng hết hạn.");
            }

            if (await _repo.HasActiveContractByAccountAsync(dto.AccountId))
            {
                throw new Exception("Tài khoản này hiện đã có hợp đồng đang có hiệu lực. Không thể tạo thêm hợp đồng mới khi hợp đồng cũ còn hiệu lực.");
            }

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

            if (dto.Status == "Expired")
            {
                throw new Exception("Không thể chuyển sang trạng thái 'Hết hiệu lực' thủ công. Trạng thái này do hệ thống tự động cập nhật khi hợp đồng hết hạn.");
            }

            if (dto.Status == "Active" && await _repo.HasActiveContractByAccountAsync(dto.AccountId, dto.Id))
            {
                throw new Exception("Tài khoản này hiện đã có hợp đồng khác đang có hiệu lực. Không thể kích hoạt hợp đồng này.");
            }

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
