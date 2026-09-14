using System.Security.Claims;
using MenuGoBE.Dtos.Promotion;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner,Manager,Admin")]
public class PromotionController : ControllerBase
{
    private readonly IPromotionService _promotionService;

    public PromotionController(IPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    private long GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("id");
        if (idClaim != null && long.TryParse(idClaim.Value, out var id))
            return id;
        return 0;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePromotion([FromBody] PromotionCreateDto dto)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized("Invalid token");

            // Auto-assign branch for managers
            var branchIdClaim = User.FindFirst("branchId") ?? User.FindFirst("BranchId");
            if (branchIdClaim != null && long.TryParse(branchIdClaim.Value, out var bId) && !User.IsInRole("Owner") && !User.IsInRole("Admin"))
            {
                dto.Scope = PromotionScope.SpecificBranches;
                if (!dto.BranchIds.Contains(bId)) {
                    dto.BranchIds.Add(bId);
                }
            }

            var result = await _promotionService.CreatePromotionAsync(dto, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPromotions([FromQuery] long? branchId)
    {
        try
        {
            var branchIdClaim = User.FindFirst("branchId") ?? User.FindFirst("BranchId");
            if (branchIdClaim != null && long.TryParse(branchIdClaim.Value, out var bId) && !User.IsInRole("Owner") && !User.IsInRole("Admin"))
            {
                var resultByBranch = await _promotionService.GetAllPromotionsByBranchAsync(bId);
                return Ok(resultByBranch);
            }

            var result = await _promotionService.GetAllPromotionsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPromotionById(long id)
    {
        try
        {
            var result = await _promotionService.GetPromotionByIdAsync(id);
            if (result == null) return NotFound(new { message = "Promotion not found." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePromotion(long id, [FromBody] PromotionUpdateDto dto)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized("Invalid token");

            // Auto-assign branch for managers
            var branchIdClaim = User.FindFirst("branchId") ?? User.FindFirst("BranchId");
            if (branchIdClaim != null && long.TryParse(branchIdClaim.Value, out var bId) && !User.IsInRole("Owner") && !User.IsInRole("Admin"))
            {
                dto.Scope = PromotionScope.SpecificBranches;
                if (!dto.BranchIds.Contains(bId)) {
                    dto.BranchIds.Add(bId);
                }
            }

            dto.Id = id;
            var result = await _promotionService.UpdatePromotionAsync(dto, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePromotion(long id)
    {
        try
        {
            await _promotionService.DeletePromotionAsync(id);
            return Ok(new { message = "Promotion deleted successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPut("{promoId}/remove-product/{productId}")]
    public async Task<IActionResult> RemoveProductFromPromotion(long promoId, long productId)
    {
        try
        {
            await _promotionService.RemoveProductFromPromotionAsync(promoId, productId);
            return Ok(new { message = "Đã gỡ sản phẩm khỏi chương trình khuyến mãi." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
