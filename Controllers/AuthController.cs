using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MenuGoBE.Data;
using MenuGoBE.Models;
using MenuGoBE.Dtos.Auth;
using Google.Authenticator;

namespace MenuGoBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Email == dto.Email && a.IsActive);

            if (account == null)
            {
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
            }

            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, account.HashedPassword);
            }
            catch
            {
                // Fallback nếu DB đang lưu plain text (trong quá trình dev)
                isPasswordValid = (dto.Password == account.HashedPassword);
            }

            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
            }

            if (string.IsNullOrEmpty(account.TwoFACode))
            {
                return Ok(new
                {
                    status = "setup_2fa_required",
                    accountId = account.Id,
                    message = "Tài khoản của bạn chưa cài đặt bảo mật 2 lớp. Bắt buộc cài đặt để tiếp tục."
                });
            }

            return Ok(new
            {
                status = "verify_2fa_required",
                accountId = account.Id,
                message = "Vui lòng nhập mã 6 số từ Google Authenticator."
            });
        }

        [HttpPost("setup-2fa")]
        public async Task<IActionResult> Setup2FA([FromBody] Setup2FADto dto)
        {
            var account = await _context.Accounts.FindAsync(dto.AccountId);
            if (account == null) return NotFound("Account not found");

            var tfa = new TwoFactorAuthenticator();
            string secretKey = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10).ToUpper();
            
            var setupInfo = tfa.GenerateSetupCode("MenuGo", account.Email, secretKey, false, 3);
            
            return Ok(new
            {
                secretKey = secretKey,
                qrCodeUrl = setupInfo.QrCodeSetupImageUrl,
                manualEntryKey = setupInfo.ManualEntryKey
            });
        }

        [HttpPost("confirm-setup-2fa")]
        public async Task<IActionResult> ConfirmSetup2FA([FromBody] ConfirmSetup2FADto dto)
        {
            var account = await _context.Accounts
                .Include(a => a.TempRoles)
                    .ThenInclude(tr => tr.Role)
                .Include(a => a.Contracts)
                    .ThenInclude(c => c.Role)
                .FirstOrDefaultAsync(a => a.Id == dto.AccountId);

            if (account == null) return NotFound("Account not found");

            var tfa = new TwoFactorAuthenticator();
            bool isCorrectPin = tfa.ValidateTwoFactorPIN(dto.SecretKey, dto.Code);

            if (!isCorrectPin)
            {
                return BadRequest(new { message = "Mã xác thực không chính xác." });
            }

            account.TwoFACode = dto.SecretKey;
            await _context.SaveChangesAsync();

            var (activeRoles, branchIds) = GetActiveRoles(account);
            var token = GenerateJwtToken(account, activeRoles, branchIds);

            return Ok(new
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
            });
        }

        [HttpPost("verify-2fa")]
        public async Task<IActionResult> Verify2FA([FromBody] Verify2FADto dto)
        {
            var account = await _context.Accounts
                .Include(a => a.TempRoles)
                    .ThenInclude(tr => tr.Role)
                .Include(a => a.Contracts)
                    .ThenInclude(c => c.Role)
                .FirstOrDefaultAsync(a => a.Id == dto.AccountId);

            if (account == null) return NotFound("Account not found");

            var tfa = new TwoFactorAuthenticator();
            bool isCorrectPin = tfa.ValidateTwoFactorPIN(account.TwoFACode, dto.Code);

            if (!isCorrectPin)
            {
                return BadRequest(new { message = "Mã xác thực không chính xác." });
            }

            var (activeRoles, branchIds) = GetActiveRoles(account);
            var token = GenerateJwtToken(account, activeRoles, branchIds);

            return Ok(new
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
            });
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

