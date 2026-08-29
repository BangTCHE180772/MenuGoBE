using MenuGoBE.Dtos.HolidayConfig;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HolidayConfigController : ControllerBase
{
    private readonly IHolidayConfigService _holidayConfigService;

    public HolidayConfigController(IHolidayConfigService holidayConfigService)
    {
        _holidayConfigService = holidayConfigService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] long? branchId = null)
    {
        var result = await _holidayConfigService.GetAllAsync(branchId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _holidayConfigService.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = $"Không tìm thấy cấu hình ngày lễ ID {id}" });
        return Ok(result);
    }

    private long? GetCurrentAccountId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) 
                 ?? User.FindFirst("sub") 
                 ?? User.FindFirst("id");
        if (claim != null && long.TryParse(claim.Value, out var id))
        {
            return id;
        }
        return null;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] HolidayConfigCreateDto dto)
    {
        try
        {
            var userId = GetCurrentAccountId();
            if (userId.HasValue && userId.Value > 0)
            {
                dto.CreatedBy = userId.Value;
            }
            else
            {
                dto.CreatedBy = null;
            }

            var result = await _holidayConfigService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] HolidayConfigUpdateDto dto)
    {
        if (id != dto.Id) return BadRequest(new { message = "ID không trùng khớp" });

        try
        {
            var success = await _holidayConfigService.UpdateAsync(dto);
            if (!success) return NotFound(new { message = $"Không tìm thấy cấu hình ngày lễ ID {id}" });
            return Ok(new { message = "Cập nhật thành công" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var success = await _holidayConfigService.DeleteAsync(id);
        if (!success) return NotFound(new { message = $"Không tìm thấy cấu hình ngày lễ ID {id}" });
        return Ok(new { message = "Xóa thành công" });
    }
}
