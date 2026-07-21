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

namespace MenuGoBE.Service
{
    public class AuthService : IAuthService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IAccountRepository accountRepository, IConfiguration configuration)
        {
            _accountRepository = accountRepository;
            _configuration = configuration;
        }

        public async Task<object> LoginAsync(LoginDto dto)
        {
            var account = await _accountRepository.GetAccountByEmailAsync(dto.Email);

            if (account == null)
            {
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
            }

            bool isPasswordValid = HashHelper.VerifyPassword(dto.Password, account.HashedPassword);

            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
            }

            if (string.IsNullOrEmpty(account.TwoFACode))
            {
                return new
                {
                    status = "setup_2fa_required",
                    accountId = account.Id,
                    message = "Tài khoản của bạn chưa cài đặt bảo mật 2 lớp. Bắt buộc cài đặt để tiếp tục."
                };
            }

            return new
            {
                status = "verify_2fa_required",
                accountId = account.Id,
                message = "Vui lòng nhập mã 6 số từ Google Authenticator."
            };
        }

        public async Task<object> Setup2FAAsync(Setup2FADto dto)
        {
            var account = await _accountRepository.GetByIdAsync(dto.AccountId);
            if (account == null) throw new ArgumentException("Account not found");

            var tfa = new TwoFactorAuthenticator();
            string secretKey = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10).ToUpper();
            
            var setupInfo = tfa.GenerateSetupCode("MenuGo", account.Email, secretKey, false, 3);
            
            return new
            {
                secretKey = secretKey,
                qrCodeUrl = setupInfo.QrCodeSetupImageUrl,
                manualEntryKey = setupInfo.ManualEntryKey
            };
        }

        public async Task<object> ConfirmSetup2FAAsync(ConfirmSetup2FADto dto)
        {
            var account = await _accountRepository.GetAccountWithRolesAsync(dto.AccountId);

            if (account == null) throw new ArgumentException("Account not found");

            var tfa = new TwoFactorAuthenticator();
            bool isCorrectPin = tfa.ValidateTwoFactorPIN(dto.SecretKey, dto.Code);

            if (!isCorrectPin)
            {
                throw new UnauthorizedAccessException("Mã xác thực không chính xác.");
            }

            account.TwoFACode = dto.SecretKey;
            await _accountRepository.SaveChangesAsync();

            var (activeRoles, branchIds) = GetActiveRoles(account);
            var token = GenerateJwtToken(account, activeRoles, branchIds);

            return new
            {
                status = "success",
                token = token,
                user = new
                {
                    id = account.Id,
                    name = account.Name,
                    email = account.Email,
                    roles = activeRoles,
                    branchIds = branchIds
                }
            };
        }

        public async Task<object> Verify2FAAsync(Verify2FADto dto)
        {
            var account = await _accountRepository.GetAccountWithRolesAsync(dto.AccountId);

            if (account == null) throw new ArgumentException("Account not found");

            var tfa = new TwoFactorAuthenticator();
            bool isCorrectPin = tfa.ValidateTwoFactorPIN(account.TwoFACode, dto.Code);

            if (!isCorrectPin)
            {
                throw new UnauthorizedAccessException("Mã xác thực không chính xác.");
            }

            var (activeRoles, branchIds) = GetActiveRoles(account);
            var token = GenerateJwtToken(account, activeRoles, branchIds);

            return new
            {
                status = "success",
                token = token,
                user = new
                {
                    id = account.Id,
                    name = account.Name,
                    email = account.Email,
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
                .Where(c => c.BranchId != null && c.BranchId > 0)
                .Select(c => (long)c.BranchId!)
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
    }
}
