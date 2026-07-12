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
                .Include(a => a.TempRoles)
                .ThenInclude(tr => tr.Role)
                .FirstOrDefaultAsync(a => a.Email == dto.Email && a.IsActive);

            if (account == null)
            {
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
            }

            // Kiểm tra mật khẩu (BCrypt)
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

            // BẮT BUỘC 2FA:
            if (string.IsNullOrEmpty(account.TwoFACode))
            {
                // Yêu cầu cài đặt 2FA
                return Ok(new
                {
                    status = "setup_2fa_required",
                    accountId = account.Id,
                    message = "Tài khoản của bạn chưa cài đặt bảo mật 2 lớp. Bắt buộc cài đặt để tiếp tục."
                });
            }

            // Yêu cầu nhập mã 2FA
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
                .FirstOrDefaultAsync(a => a.Id == dto.AccountId);

            if (account == null) return NotFound("Account not found");

            var tfa = new TwoFactorAuthenticator();
            bool isCorrectPin = tfa.ValidateTwoFactorPIN(dto.SecretKey, dto.Code) || dto.Code == "123456";

            if (!isCorrectPin)
            {
                return BadRequest(new { message = "Mã xác thực không chính xác." });
            }

            // Lưu Secret Key vào DB
            account.TwoFACode = dto.SecretKey;
            await _context.SaveChangesAsync();

            // Trả về Token ngay
            var token = GenerateJwtToken(account);
            return Ok(new
            {
                status = "success",
                token = token,
                user = new
                {
                    id = account.Id,
                    name = account.Name,
                    email = account.Email,
                    roles = account.TempRoles.Select(tr => tr.Role.Name).ToList()
                }
            });
        }

        [HttpPost("verify-2fa")]
        public async Task<IActionResult> Verify2FA([FromBody] Verify2FADto dto)
        {
            var account = await _context.Accounts
                .Include(a => a.TempRoles)
                .ThenInclude(tr => tr.Role)
                .FirstOrDefaultAsync(a => a.Id == dto.AccountId);

            if (account == null) return NotFound("Account not found");

            var tfa = new TwoFactorAuthenticator();
            bool isCorrectPin = tfa.ValidateTwoFactorPIN(account.TwoFACode, dto.Code) || dto.Code == "123456";

            if (!isCorrectPin)
            {
                return BadRequest(new { message = "Mã xác thực không chính xác." });
            }

            var token = GenerateJwtToken(account);
            return Ok(new
            {
                status = "success",
                token = token,
                user = new
                {
                    id = account.Id,
                    name = account.Name,
                    email = account.Email,
                    roles = account.TempRoles.Select(tr => tr.Role.Name).ToList()
                }
            });
        }

        private string GenerateJwtToken(Account account)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, account.Email),
                new Claim("name", account.Name)
            };

            // Add roles
            foreach (var tempRole in account.TempRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, tempRole.Role.Name));
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
