using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Google.Authenticator;
using MenuGoBE.Dtos.Auth;
using MenuGoBE.Helpers;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Caching.Memory;

namespace MenuGoBE.Service
{
    public class AuthService : IAuthService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService;

        public AuthService(
            IAccountRepository accountRepository,
            IConfiguration configuration,
            IMemoryCache cache,
            IEmailService emailService)
        {
            _accountRepository = accountRepository;
            _configuration = configuration;
            _cache = cache;
            _emailService = emailService;
        }

        public async Task<object> LoginAsync(LoginDto dto)
        {
            var account = await _accountRepository.GetAccountByEmailAsync(dto.Email.Trim().ToLower());

            if (account == null)
            {
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
            }

            bool isPasswordValid = HashHelper.VerifyPassword(dto.Password, account.HashedPassword);

            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
            }

            var accountWithRoles = await _accountRepository.GetAccountWithRolesAsync(account.Id);
            if (accountWithRoles == null)
            {
                throw new UnauthorizedAccessException("Tài khoản không tồn tại.");
            }

            var (activeRoles, branchIds) = GetActiveRoles(accountWithRoles);
            var token = GenerateJwtToken(accountWithRoles, activeRoles, branchIds);

            return new
            {
                status = "success",
                token = token,
                user = new
                {
                    id = accountWithRoles.Id,
                    name = accountWithRoles.Name,
                    email = accountWithRoles.Email,
                    roles = activeRoles,
                    branchIds = branchIds
                }
            };
        }

        private (List<string> roles, List<long> branchIds) GetActiveRoles(Account account)
        {
            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);

            var activeContracts = account.Contracts
                .Where(c => c.Status == "Active" && c.StartDate <= today && (c.EndDate == null || c.EndDate >= today))
                .ToList();

            if (!activeContracts.Any())
            {
                return (new List<string>(), new List<long>());
            }

            var branchIds = activeContracts
                .Where(c => c.BranchId > 0)
                .Select(c => c.BranchId)
                .Distinct()
                .ToList();

            var activeTempRoles = account.TempRoles
                .Where(tr => tr.Status == "Active" && tr.StartTime <= now && tr.EndTime >= now)
                .Select(tr => tr.Role.Name)
                .Distinct()
                .ToList();

            if (activeTempRoles.Any())
            {
                return (activeTempRoles, branchIds);
            }

            var contractRoles = activeContracts.Select(c => c.Role.Name).Distinct().ToList();
            return (contractRoles, branchIds);
        }

        private string GenerateJwtToken(Account account, List<string> roles, List<long> branchIds)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, account.Email),
                new Claim("name", account.Name)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
            }

            foreach (var branchId in branchIds)
            {
                claims.Add(new Claim("branchId", branchId.ToString()));
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<object> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var emailLower = dto.Email.Trim().ToLower();
            var account = await _accountRepository.GetAccountByEmailAsync(emailLower);

            // Bất kể email có tồn tại hay không, trả về thành công để tránh dò quét email (Security Practice)
            if (account != null)
            {
                var otpCode = Random.Shared.Next(100000, 999999).ToString();
                var expiredAt = DateTime.UtcNow.AddMinutes(5);

                var otpData = new ResetOtpData
                {
                    Otp = otpCode,
                    ExpiredAt = expiredAt
                };

                // Lưu OTP vào cache trong 5 phút
                var cacheKey = $"reset-otp-{emailLower}";
                _cache.Set(cacheKey, otpData, TimeSpan.FromMinutes(5));

                // Gửi Email
                var subject = "MenuGo - Password Reset Verification";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 8px;'>
                        <h2 style='color: #ff4d4f; text-align: center;'>Đặt Lại Mật Khẩu MenuGo</h2>
                        <p>Xin chào,</p>
                        <p>Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản MenuGo.</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #ff4d4f; background: #fff1f0; padding: 10px 20px; border-radius: 4px; border: 1px dashed #ffa39e;'>
                                {otpCode}
                            </span>
                        </div>
                        <p>Mã xác thực có hiệu lực trong <strong>5 phút</strong> và chỉ sử dụng được <strong>1 lần</strong>.</p>
                        <p style='color: #8c8c8c; font-size: 13px; margin-top: 30px; border-top: 1px solid #f0f0f0; padding-top: 10px;'>
                            Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua Email an toàn này.<br/>
                            Trân trọng,<br/>
                            <strong>MenuGo Team</strong>
                        </p>
                    </div>";

                await _emailService.SendEmailAsync(account.Email, subject, body);
            }

            return new { message = "Nếu email tồn tại trong hệ thống, chúng tôi sẽ gửi mã xác thực." };
        }

        public async Task<object> VerifyResetOtpAsync(VerifyResetOtpDto dto)
        {
            var emailLower = dto.Email.Trim().ToLower();
            var cacheKey = $"reset-otp-{emailLower}";

            if (!_cache.TryGetValue<ResetOtpData>(cacheKey, out var otpData) || otpData == null)
            {
                throw new ArgumentException("Mã xác thực đã hết hạn.");
            }

            if (otpData.Otp != dto.Otp.Trim())
            {
                throw new UnauthorizedAccessException("Mã xác thực không đúng.");
            }

            // OTP đúng: Lưu cờ verified trong 5 phút để cho phép đổi mật khẩu ở bước tiếp theo
            var verifiedKey = $"reset-verified-{emailLower}";
            _cache.Set(verifiedKey, true, TimeSpan.FromMinutes(5));

            // Xóa OTP ngay sau khi dùng
            _cache.Remove(cacheKey);

            return new { message = "Xác thực mã OTP thành công." };
        }

        public async Task<object> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var emailLower = dto.Email.Trim().ToLower();
            var verifiedKey = $"reset-verified-{emailLower}";

            if (!_cache.TryGetValue<bool>(verifiedKey, out var isVerified) || !isVerified)
            {
                throw new UnauthorizedAccessException("Mã xác thực đã hết hạn hoặc chưa được xác minh.");
            }

            var account = await _accountRepository.GetAccountByEmailAsync(emailLower);
            if (account == null)
            {
                throw new ArgumentException("Không tìm thấy tài khoản.");
            }

            // Cập nhật password mới
            account.HashedPassword = HashHelper.HashPassword(dto.NewPassword);
            await _accountRepository.UpdateAsync(account);
            await _accountRepository.SaveChangesAsync();

            // Dọn dẹp cache
            _cache.Remove(verifiedKey);

            return new { message = "Đổi mật khẩu thành công." };
        }

        private class ResetOtpData
        {
            public string Otp { get; set; } = string.Empty;
            public DateTime ExpiredAt { get; set; }
        }
    }
}
