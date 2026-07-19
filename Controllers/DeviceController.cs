using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MenuGoBE.Dtos.Device;
using MenuGoBE.Interface.Services;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly IDeviceService _service;

        public DeviceController(IDeviceService service)
        {
            _service = service;
        }

        [HttpPost("setup")]
        [Authorize(Roles = "Manager,Admin,Owner")]
        public async Task<IActionResult> Setup([FromBody] DeviceSetupDto dto)
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(accountIdClaim) || !long.TryParse(accountIdClaim, out var accountId))
                return Unauthorized(new { message = "Không xác định được tài khoản." });

            var userBranchIds = User.FindAll("branchId")
                .Select(c => long.TryParse(c.Value, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();

            long branchId;
            if (dto.BranchId.HasValue && dto.BranchId.Value > 0)
            {
                if (!User.IsInRole("Admin") && !User.IsInRole("Owner") && !userBranchIds.Contains(dto.BranchId.Value))
                    return Forbid();
                branchId = dto.BranchId.Value;
            }
            else
            {
                if (!userBranchIds.Any())
                    return BadRequest(new { message = "Không xác định được chi nhánh. Tài khoản cần gắn với chi nhánh." });
                branchId = userBranchIds.First();
            }

            var validTypes = new[] { "POS", "Waiter", "Kitchen", "Attendance" };
            if (!validTypes.Contains(dto.DeviceType))
                return BadRequest(new { message = $"Loại thiết bị không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validTypes)}" });

            var result = await _service.SetupAsync(dto, branchId, accountId);
            return Ok(result);
        }

        [HttpPost("validate")]
        [AllowAnonymous]
        public async Task<IActionResult> Validate([FromBody] DeviceValidateDto dto)
        {
            try
            {
                var result = await _service.ValidateAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("thu hồi"))
                    return StatusCode(403, new { message = ex.Message });
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("branch/{branchId}")]
        [Authorize(Roles = "Manager,Admin,Owner")]
        public async Task<IActionResult> GetByBranch(long branchId)
        {
            // Manager chỉ được xem thiết bị chi nhánh của mình
            if (!User.IsInRole("Admin") && !User.IsInRole("Owner"))
            {
                var userBranchIds = User.FindAll("branchId")
                    .Select(c => long.TryParse(c.Value, out var id) ? id : 0)
                    .Where(id => id > 0)
                    .ToList();
                if (!userBranchIds.Contains(branchId))
                    return Forbid();
            }

            var result = await _service.GetByBranchIdAsync(branchId);
            return Ok(result);
        }

        [HttpPut]
        [Authorize(Roles = "Manager,Admin,Owner")]
        public async Task<IActionResult> Update([FromBody] DeviceUpdateDto dto)
        {
            try
            {
                var validTypes = new[] { "POS", "Waiter", "Kitchen", "Attendance" };
                if (!validTypes.Contains(dto.DeviceType))
                    return BadRequest(new { message = $"Loại thiết bị không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validTypes)}" });

                await _service.UpdateAsync(dto);
                return Ok(new { message = "Cập nhật thành công." });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager,Admin,Owner")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { message = "Xóa thành công." });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/revoke")]
        [Authorize(Roles = "Manager,Admin,Owner")]
        public async Task<IActionResult> Revoke(long id)
        {
            try
            {
                await _service.RevokeAsync(id);
                return Ok(new { message = "Đã thu hồi quyền thiết bị." });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("employee-login")]
        [AllowAnonymous]
        public async Task<IActionResult> EmployeeLogin([FromBody] DeviceEmployeeLoginDto dto)
        {
            try
            {
                var result = await _service.EmployeeLoginAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("không tồn tại") || ex.Message.Contains("không hợp lệ") || ex.Message.Contains("không thuộc chi nhánh"))
                    return Unauthorized(new { message = ex.Message });
                    
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
