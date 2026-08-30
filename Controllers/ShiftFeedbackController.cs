using System.Security.Claims;
using MenuGoBE.Dtos.WorkSchedule;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ShiftFeedbackController : ControllerBase
{
    private readonly IShiftFeedbackService _service;

    public ShiftFeedbackController(IShiftFeedbackService service)
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
    public async Task<IActionResult> Create([FromBody] ShiftFeedbackCreateDto dto)
    {
        var accountId = GetCurrentAccountId();
        if (!accountId.HasValue)
        {
            return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });
        }

        try
        {
            var result = await _service.CreateFeedbackAsync(accountId.Value, dto);
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
        {
            return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });
        }

        var list = await _service.GetMyFeedbacksAsync(accountId.Value);
        return Ok(list);
    }

    [HttpGet("branch/{branchId}")]
    public async Task<IActionResult> GetByBranch(long branchId, [FromQuery] string? status)
    {
        var list = await _service.GetBranchFeedbacksAsync(branchId, status);
        return Ok(list);
    }

    [HttpPut("{id}/process")]
    public async Task<IActionResult> Process(long id, [FromBody] ShiftFeedbackProcessDto dto)
    {
        var accountId = GetCurrentAccountId();
        if (!accountId.HasValue)
        {
            return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });
        }

        try
        {
            var result = await _service.ProcessFeedbackAsync(id, accountId.Value, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
