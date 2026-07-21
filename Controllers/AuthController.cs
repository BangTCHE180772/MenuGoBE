using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Auth;
using MenuGoBE.Interface.Services; 

namespace MenuGoBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;

        public AuthController(IAuthService authService, IEmailService emailService)
        {
            _authService = authService;
            _emailService = emailService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }

        [HttpPost("test-mail")]
        public async Task<IActionResult> TestMail([FromQuery] string toEmail)
        {
            try
            {
                string subject = "Test Email từ MenuGo";
                string body = "<h1>Chào bạn,</h1><p>Hệ thống MenuGo đã gửi email thành công tới bạn!</p>";
                await _emailService.SendEmailAsync(toEmail, subject, body);
                return Ok(new { message = $"Đã gửi email test tới {toEmail} thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi gửi mail", error = ex.Message });
            }
        }

        [HttpPost("setup-2fa")]
        public async Task<IActionResult> Setup2FA([FromBody] Setup2FADto dto)
        {
            try
            {
                var result = await _authService.Setup2FAAsync(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }

        [HttpPost("confirm-setup-2fa")]
        public async Task<IActionResult> ConfirmSetup2FA([FromBody] ConfirmSetup2FADto dto)
        {
            try
            {
                var result = await _authService.ConfirmSetup2FAAsync(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }

        [HttpPost("verify-2fa")]
        public async Task<IActionResult> Verify2FA([FromBody] Verify2FADto dto)
        {
            try
            {
                var result = await _authService.Verify2FAAsync(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }
    }
}
