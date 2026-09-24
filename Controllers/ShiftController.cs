using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Shift;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ShiftController : ControllerBase
    {
        private readonly IShiftService _service;
        private readonly IAccountService _accountService;

        public ShiftController(IShiftService service, IAccountService accountService)
        {
            _service = service;
            _accountService = accountService;
        }

        private long? GetCurrentAccountId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("id");
            if (claim != null && long.TryParse(claim.Value, out var id))
            {
                return id;
            }
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy Shift" });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ShiftCreateDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, _, _) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin)
                return StatusCode(403, new { message = "Chỉ Chủ sở hữu mới được tạo ca làm việc." });

            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ShiftUpdateDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, _, _) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin)
                return StatusCode(403, new { message = "Chỉ Chủ sở hữu mới được sửa ca làm việc." });

            var result = await _service.UpdateAsync(dto);
            if (!result)
                return NotFound(new { message = "Không tìm thấy Shift để cập nhật" });

            return Ok(new { message = "Cập nhật thành công" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, _, _) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin)
                return StatusCode(403, new { message = "Chỉ Chủ sở hữu mới được xóa ca làm việc." });

            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = "Không tìm thấy Shift để xóa" });

            return Ok(new { message = "Xóa thành công" });
        }
    }
}
