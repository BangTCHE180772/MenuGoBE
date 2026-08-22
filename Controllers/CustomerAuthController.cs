using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Auth;
using MenuGoBE.Interface.Services;

namespace MenuGoBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerAuthController : ControllerBase
    {
        private readonly ICustomerAuthService _authService;

        public CustomerAuthController(ICustomerAuthService authService)
        {
            _authService = authService;
        }

        #region POST Đăng ký tài khoản khách hàng
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CustomerRegisterDto dto)
        {
            try
            {
                await _authService.RegisterAsync(dto);
                return Ok(new { success = true, message = "Đăng ký tài khoản thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }
        #endregion

        #region POST Đăng nhập tài khoản khách hàng
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] CustomerLoginDto dto)
        {
            try
            {
                var response = await _authService.LoginAsync(dto);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(new { message = ex.Message });
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
        #endregion

        #region POST Gửi mã OTP khôi phục mật khẩu qua Email
        [HttpPost("forgot-password/send-otp")]
        public async Task<IActionResult> SendResetPasswordOtp([FromBody] CustomerSendOtpRequestDto dto)
        {
            try
            {
                await _authService.SendResetPasswordOtpAsync(dto.Email);
                return Ok(new { success = true, message = "Mã xác nhận (OTP) đã được gửi tới email của bạn." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }
        #endregion

        #region POST Xác nhận OTP và đặt lại mật khẩu mới
        [HttpPost("forgot-password/reset")]
        public async Task<IActionResult> ResetPasswordWithOtp([FromBody] CustomerResetPasswordDto dto)
        {
            try
            {
                await _authService.ResetPasswordWithOtpAsync(dto);
                return Ok(new { success = true, message = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập ngay bây giờ." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }
        #endregion
    }
}
