using MenuGoBE.Dtos.Voucher;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VoucherController : ControllerBase
{
    private readonly IVoucherService _service;

    public VoucherController(IVoucherService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] long? branchId)
    {
        var branchIdClaim = User.FindFirst("branchId") ?? User.FindFirst("BranchId");
        if (branchIdClaim != null && long.TryParse(branchIdClaim.Value, out var bId) && !User.IsInRole("Owner") && !User.IsInRole("Admin"))
        {
            var result = await _service.GetByBranchIdAsync(bId);
            return Ok(result);
        }
        var allResult = await _service.GetAllAsync();
        return Ok(allResult);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Không tìm thấy Voucher" });
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VoucherCreateDto dto)
    {
        try
        {
            var branchIdClaim = User.FindFirst("branchId") ?? User.FindFirst("BranchId");
            if (branchIdClaim != null && long.TryParse(branchIdClaim.Value, out var bId) && !User.IsInRole("Owner") && !User.IsInRole("Admin"))
            {
                dto.BranchId = bId;
            }

            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] VoucherUpdateDto dto)
    {
        try
        {
            var branchIdClaim = User.FindFirst("branchId") ?? User.FindFirst("BranchId");
            if (branchIdClaim != null && long.TryParse(branchIdClaim.Value, out var bId) && !User.IsInRole("Owner") && !User.IsInRole("Admin"))
            {
                dto.BranchId = bId;
            }

            var result = await _service.UpdateAsync(dto);
            if (!result) return NotFound(new { message = "Không tìm thấy Voucher để cập nhật" });
            return Ok(new { message = "Cập nhật thành công" });
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
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound(new { message = "Không tìm thấy Voucher để xóa" });
            return Ok(new { message = "Xóa thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("check")]
    public async Task<IActionResult> CheckVoucher([FromQuery] string code, [FromQuery] long branchId, [FromQuery] decimal totalAmount, [FromQuery] long? customerId = null)
    {
        var result = await _service.CheckVoucherAsync(code, branchId, totalAmount, customerId);
        if (!result.IsValid)
        {
            return BadRequest(new { message = result.Message });
        }
        return Ok(result);
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable()
    {
        long? customerId = null;
        var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirst("id");
        if (idClaim != null && long.TryParse(idClaim.Value, out var id))
        {
            customerId = id;
        }

        var result = await _service.GetAvailableAsync(customerId);
        return Ok(result);
    }
}
