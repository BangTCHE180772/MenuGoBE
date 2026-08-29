using System.Security.Claims;
using MenuGoBE.Dtos.WorkSchedule;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ShiftChangeRequestController : ControllerBase
    {
        private readonly IShiftChangeRequestService _service;

        public ShiftChangeRequestController(IShiftChangeRequestService service)
        {
            _service = service;
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ShiftChangeRequestCreateDto dto)
        {
            var accountId = GetCurrentAccountId();
            if (!accountId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            try
            {
                var result = await _service.CreateAsync(accountId.Value, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var accountId = GetCurrentAccountId();
            if (!accountId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var result = await _service.GetMineAsync(accountId.Value);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}")]
        public async Task<IActionResult> GetByBranch(long branchId, [FromQuery] ShiftChangeRequestStatus? status)
        {
            var result = await _service.GetByBranchAsync(branchId, status);
            return Ok(result);
        }

        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(long id)
        {
            var accountId = GetCurrentAccountId();
            if (!accountId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var result = await _service.ApproveAsync(id, accountId.Value);
            if (!result)
                return NotFound(new { message = "Không tìm thấy yêu cầu hoặc yêu cầu đã được xử lý." });

            return Ok(new { message = "Đã duyệt yêu cầu đổi ca." });
        }

        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(long id, [FromBody] ShiftChangeRejectDto dto)
        {
            var accountId = GetCurrentAccountId();
            if (!accountId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var result = await _service.RejectAsync(id, accountId.Value, dto?.Reason);
            if (!result)
                return NotFound(new { message = "Không tìm thấy yêu cầu hoặc yêu cầu đã được xử lý." });

            return Ok(new { message = "Đã từ chối yêu cầu đổi ca." });
        }
    }
}
