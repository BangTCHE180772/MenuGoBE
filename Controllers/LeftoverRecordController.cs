using System.Security.Claims;
using MenuGoBE.Dtos.Leftover;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LeftoverRecordController : ControllerBase
    {
        private readonly ILeftoverRecordService _service;

        public LeftoverRecordController(ILeftoverRecordService service)
        {
            _service = service;
        }

        private long? GetCurrentAccountId()
        {
            var sub = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                   ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("id")?.Value;

            if (long.TryParse(sub, out var accountId))
                return accountId;

            return null;
        }

        // GET /api/LeftoverRecord/branch/{branchId}?type=Return&fromDate=2026-08-01&toDate=2026-08-31
        [HttpGet("branch/{branchId}")]
        public async Task<IActionResult> GetByBranch(long branchId, [FromQuery] LeftoverType? type, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
        {
            var result = await _service.GetByBranchAsync(branchId, type, fromDate, toDate);
            return Ok(result);
        }

        // GET /api/LeftoverRecord/branch/{branchId}/reusable?productId=123
        // Món thừa (khách trả, bếp đã làm xong) trong 30p gần nhất, chưa được dùng lại —
        // dùng làm gợi ý khi có đơn khác cần đúng món đó.
        [HttpGet("branch/{branchId}/reusable")]
        public async Task<IActionResult> GetReusable(long branchId, [FromQuery] long? productId)
        {
            var result = await _service.GetReusableAsync(branchId, productId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LeftoverRecordCreateDto dto)
        {
            var result = await _service.CreateAsync(dto, GetCurrentAccountId());
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = "Không tìm thấy bản ghi" });

            return Ok(new { message = "Xóa thành công" });
        }
    }
}
