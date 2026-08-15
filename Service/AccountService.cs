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
        private readonly IEmailService _emailService;

        public AccountService(IAccountRepository repo, IMapper mapper, IEmailService emailService)
        {
            _repo = repo;
            _mapper = mapper;
            _emailService = emailService;
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
            dto.Email = dto.Email.Trim().ToLower();
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

            // Gửi email chứa thông tin tài khoản (email và mật khẩu) về email đăng ký
            try
            {
                string subject = "Thông tin tài khoản đăng ký - MenuGo";
                string body = $@"
                    <div style=""font-family: Arial, sans-serif; font-size: 15px; color: #333; line-height: 1.6; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px; background-color: #ffffff;"">
                        <div style=""text-align: center; padding-bottom: 15px; border-bottom: 2px solid #4A90E2;"">
                            <h2 style=""color: #4A90E2; margin: 0;"">Chào mừng bạn đến với MenuGo!</h2>
                        </div>
                        <div style=""padding: 20px 0;"">
                            <p>Xin chào <strong>{dto.Name}</strong>,</p>
                            <p>Tài khoản của bạn đã được khởi tạo thành công trên hệ thống <strong>MenuGo</strong>. Dưới đây là thông tin đăng nhập của bạn:</p>
                            <div style=""background-color: #f8f9fa; padding: 15px; border-left: 4px solid #4A90E2; border-radius: 4px; margin: 15px 0;"">
                                <p style=""margin: 5px 0;""><strong>Email đăng ký:</strong> <span style=""color: #2c3e50;"">{dto.Email}</span></p>
                                <p style=""margin: 5px 0;""><strong>Mật khẩu:</strong> <span style=""color: #d9534f; font-weight: bold;"">{dto.Password}</span></p>
                            </div>
                            <p>Vì lý do bảo mật, vui lòng đăng nhập vào hệ thống và thay đổi mật khẩu của bạn trong lần đầu tiên sử dụng.</p>
                        </div>
                        <div style=""border-top: 1px solid #eeeeee; padding-top: 15px; text-align: center; color: #777; font-size: 13px;"">
                            <p style=""margin: 0;"">Đây là email tự động từ hệ thống MenuGo. Vui lòng không trả lời email này.</p>
                        </div>
                    </div>";

                await _emailService.SendEmailAsync(dto.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AccountService] Gửi email thất bại tới {dto.Email}: {ex.Message}");
            }

            return _mapper.Map<AccountViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(AccountUpdateDto dto)
        {
            dto.Email = dto.Email.Trim().ToLower();
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            // Validate uniqueness only if values have changed (and exclude current account ID)
            if (!string.Equals(entity.Email, dto.Email, StringComparison.OrdinalIgnoreCase) && await _repo.ExistsByEmailAsync(dto.Email, dto.Id))
                throw new InvalidOperationException("Email đã được sử dụng.");
            if (entity.Phone != dto.Phone && await _repo.ExistsByPhoneAsync(dto.Phone, dto.Id))
                throw new InvalidOperationException("Số điện thoại đã được sử dụng.");
            if (!string.IsNullOrWhiteSpace(dto.CitizenIdCode) && entity.CitizenIdCode != dto.CitizenIdCode && await _repo.ExistsByCitizenIdCodeAsync(dto.CitizenIdCode, dto.Id))
                throw new InvalidOperationException("Số CCCD đã được sử dụng.");

            _mapper.Map(dto, entity);

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task ChangePasswordAsync(long accountId, string currentPassword, string newPassword)
        {
            var entity = await _repo.GetByIdAsync(accountId);
            if (entity == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản.");

            if (!HashHelper.VerifyPassword(currentPassword, entity.HashedPassword))
                throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");

            entity.HashedPassword = HashHelper.HashPassword(newPassword);

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var hasActiveContract = entity.Contracts != null && entity.Contracts.Any(c =>
                string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase)
                && c.StartDate <= today
                && (!c.EndDate.HasValue || c.EndDate.Value >= today));

            if (hasActiveContract)
            {
                throw new InvalidOperationException($"Không thể xóa tài khoản '{entity.Name}' vì nhân viên đang có hợp đồng làm việc còn hiệu lực.");
            }

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
