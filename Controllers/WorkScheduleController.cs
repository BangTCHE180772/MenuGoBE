using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MenuGoBE.Dtos.WorkSchedule;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WorkScheduleController : ControllerBase
    {
        private readonly IWorkScheduleService _service;
        private readonly IAccountService _accountService;

        public WorkScheduleController(IWorkScheduleService service, IAccountService accountService)
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
        public async Task<IActionResult> GetAll([FromQuery] long? branchId, [FromQuery] long? accountId)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);

            if (isManager)
            {
                var result = await _service.GetAllAsync();
                var filtered = result.Where(w => branchIds.Contains(w.BranchId));
                if (branchId.HasValue) filtered = filtered.Where(w => w.BranchId == branchId.Value);
                if (accountId.HasValue) filtered = filtered.Where(w => w.AccountId == accountId.Value);
                return Ok(filtered.ToList());
            }
            else if (isAdmin)
            {
                var result = await _service.GetAllAsync();
                if (branchId.HasValue) result = result.Where(w => w.BranchId == branchId.Value).ToList();
                if (accountId.HasValue) result = result.Where(w => w.AccountId == accountId.Value).ToList();
                return Ok(result);
            }
            else
            {
                var result = await _service.GetByAccountIdAsync(currentUserId.Value);
                return Ok(result);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);

            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy lịch làm việc" });

            if (isManager && !branchIds.Contains(result.BranchId))
            {
                return StatusCode(403, new { message = "Bạn không có quyền xem lịch làm việc thuộc chi nhánh khác." });
            }
            if (!isAdmin && !isManager && result.AccountId != currentUserId.Value)
            {
                return StatusCode(403, new { message = "Bạn không có quyền xem lịch làm việc của nhân viên khác." });
            }

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] WorkScheduleCreateDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);

            if (isAdmin)
            {
                return StatusCode(403, new { message = "Chủ sở hữu / Quản trị viên chỉ có quyền xem lịch làm việc, việc xếp lịch thuộc về Quản lý chi nhánh." });
            }
            if (!isManager)
            {
                return StatusCode(403, new { message = "Chỉ Quản lý mới được tạo lịch làm việc." });
            }

            if (!branchIds.Contains(dto.BranchId))
            {
                return StatusCode(403, new { message = "Bạn chỉ có quyền tạo lịch làm việc tại chi nhánh do mình quản lý." });
            }

            if (dto.CreatedBy <= 0) dto.CreatedBy = currentUserId.Value;

            try
            {
                var result = await _service.CreateAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignBulk([FromBody] WorkScheduleAssignDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);

            if (isAdmin)
            {
                return StatusCode(403, new { message = "Chủ sở hữu / Quản trị viên chỉ có quyền xem lịch làm việc, việc xếp lịch thuộc về Quản lý chi nhánh." });
            }
            if (!isManager)
            {
                return StatusCode(403, new { message = "Chỉ Quản lý mới được phân công ca làm việc hàng loạt." });
            }

            if (!branchIds.Contains(dto.BranchId))
            {
                return StatusCode(403, new { message = "Bạn chỉ có quyền phân công lịch làm việc tại chi nhánh do mình quản lý." });
            }

            if (dto.CreatedBy <= 0) dto.CreatedBy = currentUserId.Value;

            try
            {
                var result = await _service.AssignBulkAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("auto-assign")]
        public async Task<IActionResult> AutoAssign([FromBody] WorkScheduleAutoAssignDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);

            if (isAdmin)
            {
                return StatusCode(403, new { message = "Chủ sở hữu / Quản trị viên chỉ có quyền xem lịch làm việc, việc xếp lịch thuộc về Quản lý chi nhánh." });
            }
            if (!isManager)
            {
                return StatusCode(403, new { message = "Chỉ Quản lý mới được tự động phân công ca làm việc." });
            }

            if (!branchIds.Contains(dto.BranchId))
            {
                return StatusCode(403, new { message = "Bạn chỉ có quyền phân công lịch làm việc tại chi nhánh do mình quản lý." });
            }

            if (dto.CreatedBy <= 0) dto.CreatedBy = currentUserId.Value;

            try
            {
                var result = await _service.AutoAssignAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] WorkScheduleUpdateDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);

            var existing = await _service.GetByIdAsync(dto.Id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy lịch làm việc để cập nhật" });

            if (isAdmin)
            {
                // Admin is allowed to update (e.g. to approve leave requests or shift swaps)
            }
            else if (isManager)
            {
                if (!branchIds.Contains(existing.BranchId) || !branchIds.Contains(dto.BranchId))
                {
                    return StatusCode(403, new { message = "Bạn chỉ có quyền sửa lịch làm việc thuộc chi nhánh của mình." });
                }
            }
            else
            {
                if (existing.AccountId != currentUserId.Value && dto.AccountId != currentUserId.Value)
                {
                    return StatusCode(403, new { message = "Bạn chỉ có quyền điều chuyển hoặc cập nhật ca làm việc của chính mình." });
                }
            }

            try
            {
                var result = await _service.UpdateAsync(dto);
                if (!result)
                    return NotFound(new { message = "Không tìm thấy lịch làm việc để cập nhật" });

                return Ok(new { message = "Cập nhật lịch làm việc thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id, [FromBody] CancelWorkScheduleDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);

            if (isAdmin)
            {
                return StatusCode(403, new { message = "Chủ sở hữu / Quản trị viên chỉ có quyền xem lịch làm việc, việc xếp lịch thuộc về Quản lý chi nhánh." });
            }
            if (!isManager)
            {
                return StatusCode(403, new { message = "Chỉ Quản lý mới được xóa lịch làm việc." });
            }

            var existing = await _service.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy lịch làm việc để xóa" });

            if (!branchIds.Contains(existing.BranchId))
            {
                return StatusCode(403, new { message = "Bạn chỉ có quyền xóa lịch làm việc thuộc chi nhánh của mình." });
            }

            try
            {
                var result = await _service.DeleteAsync(id, dto?.Reason ?? string.Empty, currentUserId.Value);
                if (!result)
                    return NotFound(new { message = "Không tìm thấy lịch làm việc để xóa" });

                return Ok(new { message = "Xóa lịch làm việc thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("mine")]
        public async Task<IActionResult> GetMySchedule()
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var result = await _service.GetByAccountIdAsync(currentUserId.Value);
            return Ok(result);
        }

        [HttpPost("leave-request")]
        public async Task<IActionResult> RequestLeave([FromBody] LeaveRequestDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            try
            {
                var success = await _service.RequestLeaveAsync(currentUserId.Value, dto);
                if (!success)
                    return BadRequest(new { message = "Không thể gửi đơn xin nghỉ. Có thể ca làm việc không tồn tại hoặc không thuộc về bạn." });

                return Ok(new { message = "Đã gửi đơn xin nghỉ thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("shift-swap")]
        public async Task<IActionResult> RequestShiftSwap([FromBody] ShiftSwapRequestDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            try
            {
                var success = await _service.RequestShiftSwapAsync(currentUserId.Value, dto);
                if (!success)
                    return BadRequest(new { message = "Không thể gửi yêu cầu đổi ca. Vui lòng kiểm tra lại thông tin." });

                return Ok(new { message = "Đã gửi yêu cầu đổi ca thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/reject-transfer")]
        public async Task<IActionResult> RejectTransfer(long id, [FromBody] RejectShiftSwapDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            try
            {
                var result = await _service.RejectShiftSwapAsync(id, dto.SenderAccountId, dto.Note, currentUserId.Value);
                if (!result)
                    return NotFound(new { message = "Không tìm thấy lịch làm việc để từ chối đổi ca." });

                return Ok(new { message = "Đã từ chối/hủy yêu cầu đổi ca." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("approve-ot")]
        public async Task<IActionResult> ApproveOT([FromBody] WorkScheduleOTApproveDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, _) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
            {
                return StatusCode(403, new { message = "Chỉ Quản lý hoặc Chủ cửa hàng mới được phê duyệt tăng ca (OT)." });
            }

            try
            {
                var success = await _service.ApproveOTAsync(dto);
                if (!success) return NotFound(new { message = "Không tìm thấy lịch làm việc để phê duyệt OT." });
                return Ok(new { message = "Cập nhật phê duyệt OT thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/verify-pos-activity")]
        public async Task<IActionResult> VerifyPOSActivity(long id)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            try
            {
                var result = await _service.VerifyPOSActivityAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Điểm danh có xác minh vị trí GPS. Được gọi từ thiết bị di động / trình duyệt của nhân viên.
        /// </summary>
        [HttpPost("check-in-with-location")]
        public async Task<IActionResult> CheckInWithLocation([FromBody] CheckInWithLocationDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            try
            {
                var (success, msg, action) = await _service.CheckInWithLocationAsync(
                    currentUserId.Value, dto.BranchId, dto.Latitude, dto.Longitude, dto.WorkScheduleId);

                return Ok(new { success, message = msg, action });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("{id}/activity-log")]
        [Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<IActionResult> GetActivityLog(long id)
        {
            try
            {
                var logs = await _service.GetActivityLogAsync(id);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
