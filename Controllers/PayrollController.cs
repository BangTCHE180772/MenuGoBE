using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Payroll;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PayrollController : ControllerBase
    {
        private readonly IPayrollService _payrollService;

        public PayrollController(IPayrollService payrollService)
        {
            _payrollService = payrollService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] long? branchId, [FromQuery] int? month, [FromQuery] int? year, [FromQuery] long? accountId)
        {
            var result = await _payrollService.GetAllAsync(branchId, month, year, accountId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _payrollService.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = $"Không tìm thấy bảng lương có ID {id}" });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PayrollCreateDto dto)
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

                var result = await _payrollService.CreateAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] PayrollGenerateDto dto)
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

                var result = await _payrollService.GenerateEmployeePayrollAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] PayrollUpdateDto dto)
        {
            try
            {
                var result = await _payrollService.UpdateAsync(dto);
                if (!result)
                    return NotFound(new { message = "Không tìm thấy bảng lương để cập nhật" });

                return Ok(new { message = "Cập nhật bảng lương thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(long id, [FromQuery] PayrollStatus status, [FromQuery] DateTime? paymentDate)
        {
            try
            {
                var result = await _payrollService.UpdateStatusAsync(id, status, paymentDate);
                if (!result)
                    return NotFound(new { message = $"Không tìm thấy bảng lương ID {id}" });

                return Ok(new { message = $"Cập nhật trạng thái bảng lương thành {status} thành công" });
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
                var result = await _payrollService.DeleteAsync(id);
                if (!result)
                    return NotFound(new { message = $"Không tìm thấy bảng lương ID {id} để xóa" });

                return Ok(new { message = "Xóa bảng lương thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
