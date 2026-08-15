using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MenuGoBE.Dtos.Device;
using MenuGoBE.Interface.Services;
using MenuGoBE.Service;
using Microsoft.AspNetCore.SignalR;
using MenuGoBE.Hubs;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly IDeviceService _service;
        private readonly IDeviceAuthService _authService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IWorkScheduleService _workScheduleService;

        public DeviceController(IDeviceService service, IDeviceAuthService authService, IHubContext<NotificationHub> hubContext, IWorkScheduleService workScheduleService)
        {
            _service = service;
            _authService = authService;
            _hubContext = hubContext;
            _workScheduleService = workScheduleService;
        }

        [HttpGet("debug-claims")]
        [Authorize]
        public IActionResult DebugClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var isAdmin = User.IsInRole("Admin");
            var isOwner = User.IsInRole("Owner");
            return Ok(new { claims, isAdmin, isOwner });
        }

        [HttpPost("setup")]
        [Authorize(Roles = "Manager,Admin,Owner")]
        public async Task<IActionResult> Setup([FromBody] DeviceSetupDto dto)
        {
            var accountIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub" || c.Type == "id")?.Value;
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

            try
            {
                var result = await _service.SetupAsync(dto, branchId, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
                if (ex.Message.Contains("tồn tại"))
                    return BadRequest(new { message = ex.Message });
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

        [HttpPost("auth/challenge")]
        [AllowAnonymous]
        public IActionResult CreateChallenge([FromBody] DeviceCreateChallengeDto dto)
        {
            if (string.IsNullOrEmpty(dto.DeviceToken))
            {
                return BadRequest(new { message = "Thiếu DeviceToken." });
            }

            var challenge = _authService.CreateChallenge(dto.DeviceToken);
            return Ok(new DeviceAuthChallengeDto
            {
                ChallengeId = challenge.ChallengeId,
                ExpiresAt = challenge.ExpiresAt
            });
        }

        [HttpPost("personal-login")]
        [Authorize]
        public async Task<IActionResult> PersonalLogin([FromBody] DeviceScanChallengeDto dto)
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value;
            var name = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "name")?.Value;

            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { message = "Không xác định được email tài khoản." });

            var challenge = _authService.GetChallenge(dto.ChallengeId);
            if (challenge == null)
            {
                return BadRequest(new { message = "Mã QR đã hết hạn hoặc không hợp lệ." });
            }

            var device = await _service.GetDeviceByTokenAsync(challenge.DeviceToken);
            if (device == null)
            {
                return BadRequest(new { message = "Thiết bị cấp mã QR không hợp lệ." });
            }

            if (device.DeviceType == "POS")
            {
                try
                {
                    var result = await _service.PersonalLoginAsync(challenge.DeviceToken, email);

                    var success = _authService.MarkAsScanned(dto.ChallengeId, email, name!);
                    if (!success)
                        return BadRequest(new { message = "Mã QR đã hết hạn." });

                    await _hubContext.Clients.Group($"device_{challenge.DeviceToken}")
                        .SendAsync("DeviceAuthScanned", result);

                    return Ok(new
                    {
                        success = true,
                        isPos = true,
                        message = "Đã gửi yêu cầu đăng nhập POS."
                    });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }
            else if (device.DeviceType == "Attendance")
            {
                var accountIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub" || c.Type == "id" || c.Type == "AccountId")?.Value;
                if (string.IsNullOrEmpty(accountIdStr) || !long.TryParse(accountIdStr, out var accountId))
                {
                    return BadRequest(new { message = "Không xác định được ID tài khoản." });
                }

                var (success, msg, action) = await _workScheduleService.RecordAttendanceAsync(accountId, device.BranchId);
                
                // KHÔNG XÓA Challenge ở đây đối với Attendance
                // Để các nhân viên khác xếp hàng phía sau có thể quét chung 1 mã QR trên màn hình
                // _authService.RemoveChallenge(dto.ChallengeId);
                
                if (success)
                {
                    await _hubContext.Clients.Group($"device_{challenge.DeviceToken}")
                        .SendAsync("AttendanceSuccess", new { name, action, message = msg });
                }

                return Ok(new
                {
                    success = success,
                    isPos = false,
                    isAttendance = true,
                    branchId = device.BranchId,
                    message = msg
                });
            }
            else
            {
                _authService.RemoveChallenge(dto.ChallengeId);
                return Ok(new
                {
                    success = true,
                    isPos = false,
                    branchId = device.BranchId,
                    message = "Đã nhận ca phục vụ thành công."
                });
            }
        }
    }
}
