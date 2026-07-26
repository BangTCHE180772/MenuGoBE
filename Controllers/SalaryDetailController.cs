using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MenuGoBE.Dtos.SalaryDetail;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalaryDetailController : ControllerBase
    {
        private readonly ISalaryDetailService _salaryDetailService;

        public SalaryDetailController(ISalaryDetailService salaryDetailService)
        {
            _salaryDetailService = salaryDetailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] long? payrollId, [FromQuery] long? accountId)
        {
            var result = await _salaryDetailService.GetAllAsync(payrollId, accountId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _salaryDetailService.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = $"Không tìm thấy chi tiết lương có ID {id}" });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SalaryDetailCreateDto dto)
        {
            try
            {
                if (dto.CreatedBy <= 0)
                {
                    var userIdClaim = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (long.TryParse(userIdClaim, out long userId))
                    {
                        dto.CreatedBy = userId;
                    }
                }

                var result = await _salaryDetailService.CreateAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] SalaryDetailUpdateDto dto)
        {
            try
            {
                var result = await _salaryDetailService.UpdateAsync(dto);
                if (!result)
                    return NotFound(new { message = "Không tìm thấy chi tiết lương để cập nhật" });

                return Ok(new { message = "Cập nhật chi tiết lương thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _salaryDetailService.DeleteAsync(id);
                if (!result)
                    return NotFound(new { message = $"Không tìm thấy chi tiết lương ID {id} để xóa" });

                return Ok(new { message = "Xóa chi tiết lương thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
