using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Models.Enums;
using MenuGoBE.Dtos.Partner;
using MenuGoBE.Dtos.Base;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartnerController : BaseApiController
    {
        private readonly IPartnerService _partnerService;

        public PartnerController(IPartnerService partnerService)
        {
            _partnerService = partnerService;
        }

        // 1. GetAll
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PartnerQueryDto query)
        {
            if (query.BranchId.HasValue && query.BranchId.Value > 0)
            {
                if (!ValidateUserAndBranchToken(query.BranchId.Value, out _, out var branchError))
                {
                    return branchError!;
                }
            }
            else
            {
                if (!ValidateUserToken(out _, out var tokenError))
                {
                    return tokenError!;
                }
            }
            var result = await _partnerService.GetPagedPartnersAsync(query);
            return Ok(result);
        }

        // 2. GetByBranchId
        [HttpGet("branch/{branchId}")]
        public async Task<IActionResult> GetByBranchId(
            long branchId,
            [FromQuery] string? search,
            [FromQuery] int? mode)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.GetPartnersByBranchAsync(branchId, search, mode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 2b. GetSuppliers (Mode 1: Supplier)
        [HttpGet("suppliers")]
        public async Task<IActionResult> GetSuppliers(
            [FromQuery] long branchId,
            [FromQuery] string? search)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.GetPartnersByBranchAsync(branchId, search, mode: 1);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 2c. GetExtraPartners (Mode 2: Transporter & Other)
        [HttpGet("extra-partners")]
        public async Task<IActionResult> GetExtraPartners(
            [FromQuery] long branchId,
            [FromQuery] string? search)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.GetPartnersByBranchAsync(branchId, search, mode: 2);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 3. GetByBranchIdAndMode
        [HttpGet("branch/{branchId}/mode/{mode}")]
        public async Task<IActionResult> GetByBranchIdAndMode(
            long branchId,
            int mode,
            [FromQuery] string? search)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.GetPartnersByBranchAsync(branchId, search, mode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 4. Create (NeedToProvide BranchId)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PartnerCreateDto dto)
        {
            if (!ValidateUserAndBranchToken(dto.BranchId, out long userId, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.CreatePartnerAsync(dto, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 4. Update (Chỉ dựa vào partnerId, không cập nhật BranchId & Type)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] PartnerUpdateDto dto)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.UpdatePartnerAsync(id, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 5. Delete By Id
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                await _partnerService.DeletePartnerAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
