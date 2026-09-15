using MenuGoBE.Dtos.Kitchen;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class KitchenController : ControllerBase
{
    private readonly IKitchenService _kitchenService;

    public KitchenController(IKitchenService kitchenService)
    {
        _kitchenService = kitchenService;
    }

    [HttpPost("request-restock")]
    public async Task<IActionResult> RequestRestock([FromBody] RequestRestockDto dto)
    {
        try
        {
            var accountIdClaim = User.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
            long? accountId = null;
            if (long.TryParse(accountIdClaim, out var parsedId))
            {
                accountId = parsedId;
            }

            var result = await _kitchenService.RequestRestockAsync(dto, accountId);
            if (result)
            {
                return Ok(new { Message = "Yêu cầu sản xuất thêm đã được gửi đến Bếp." });
            }
            return BadRequest(new { Message = "Không thể gửi yêu cầu sản xuất thêm." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
