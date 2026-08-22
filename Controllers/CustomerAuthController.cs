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
    }
}
