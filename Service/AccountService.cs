using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Account;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Helpers;

namespace MenuGoBE.Service
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _repo;
        private readonly IMapper _mapper;

        public AccountService(IAccountRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<AccountViewDto>> GetAccountsExcludeRolesAsync(List<long> excludedRoles, List<long>? branchIds = null, long? managerAccountId = null)
        {
            var accounts = await _repo.GetAccountsExcludeRolesAsync(excludedRoles, branchIds, managerAccountId);
            return _mapper.Map<List<AccountViewDto>>(accounts);
        }

        public async Task<List<AccountViewDto>> GetAccountsByRolesAsync(List<long> roleIds, long? branchId = null)
        {
            var accounts = await _repo.GetAccountsByRolesAsync(roleIds, branchId);
            return _mapper.Map<List<AccountViewDto>>(accounts);
        }

        public async Task<AccountViewDto?> GetByIdAsync(long id)
        {
            var account = await _repo.GetByIdAsync(id);
            if (account == null) return null;

            return _mapper.Map<AccountViewDto>(account);
        }

        public async Task<AccountViewDto> CreateAsync(AccountCreateDto dto, long creatorAccountId = 0)
        {
            // Validate uniqueness
            if (await _repo.ExistsByEmailAsync(dto.Email))
                throw new InvalidOperationException("Email đã được sử dụng.");
            if (await _repo.ExistsByPhoneAsync(dto.Phone))
                throw new InvalidOperationException("Số điện thoại đã được sử dụng.");
            if (await _repo.ExistsByCitizenIdCodeAsync(dto.CitizenIdCode))
                throw new InvalidOperationException("Số CCCD đã được sử dụng.");

            var entity = _mapper.Map<Account>(dto);
            entity.HashedPassword = HashHelper.HashPassword(dto.Password);
            entity.CreatedAt = DateTime.UtcNow;
            if (creatorAccountId > 0)
            {
                entity.CreatedBy = creatorAccountId;
            }

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            return _mapper.Map<AccountViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(AccountUpdateDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            // Validate uniqueness (exclude current account)
            if (await _repo.ExistsByEmailAsync(dto.Email, dto.Id))
                throw new InvalidOperationException("Email đã được sử dụng.");
            if (await _repo.ExistsByPhoneAsync(dto.Phone, dto.Id))
                throw new InvalidOperationException("Số điện thoại đã được sử dụng.");
            if (await _repo.ExistsByCitizenIdCodeAsync(dto.CitizenIdCode, dto.Id))
                throw new InvalidOperationException("Số CCCD đã được sử dụng.");

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

        public async Task<(bool IsAdmin, bool IsManager, List<long> BranchIds)> GetAccountPermissionsAsync(long accountId)
        {
            return await _repo.GetAccountPermissionsAsync(accountId);
        }

        public async Task<bool> CanManagerAccessAccountAsync(long targetAccountId, List<long> managerBranchIds, long managerAccountId)
        {
            return await _repo.CanManagerAccessAccountAsync(targetAccountId, managerBranchIds, managerAccountId);
        }
    }
}
