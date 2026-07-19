using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MenuGoBE.Dtos.WorkSchedule;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkScheduleController : ControllerBase
    {
        private readonly IWorkScheduleService _service;

        public WorkScheduleController(IWorkScheduleService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy WorkSchedule" });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] WorkScheduleCreateDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignBulk([FromBody] WorkScheduleAssignDto dto)
        {
            try
            {
                var result = await _service.AssignBulkAsync(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] WorkScheduleUpdateDto dto)
        {
            var result = await _service.UpdateAsync(dto);
            if (!result)
                return NotFound(new { message = "Không tìm thấy WorkSchedule để cập nhật" });

            return Ok(new { message = "Cập nhật thành công" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = "Không tìm thấy WorkSchedule để xóa" });

            return Ok(new { message = "Xóa thành công" });
        }

        [Authorize]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMySchedule()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountIdClaim) || !long.TryParse(accountIdClaim, out var accountId))
                return Unauthorized(new { message = "Không xác định được tài khoản." });

            var result = await _service.GetByAccountIdAsync(accountId);
            return Ok(result);
        }
    }
}
