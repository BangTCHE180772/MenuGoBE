using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Models.Enums;
using MenuGoBE.Dtos.Partner;
using MenuGoBE.Dtos.Base;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartnerController : ControllerBase
    {
        private readonly IPartnerService _partnerService;

        public PartnerController(IPartnerService partnerService)
        {
            _partnerService = partnerService;
        }

        // 1. GetAll
        [HttpGet]
        public async Task<ActionResult<PagedResult<PartnerViewDto>>> GetAll([FromQuery] PartnerQueryDto query)
        {
            var result = await _partnerService.GetPagedPartnersAsync(query);
            return Ok(result);
        }

        // 2. GetByBranchId
        [HttpGet("branch/{branchId}")]
        public async Task<ActionResult<List<PartnerViewDto>>> GetByBranchId(
            long branchId,
            [FromQuery] string? search)
        {
            try
            {
                var result = await _partnerService.GetPartnersByBranchAsync(branchId, search, mode: null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 3. GetByBranchIdAndMode
        [HttpGet("branch/{branchId}/mode/{mode}")]
        public async Task<ActionResult<List<PartnerViewDto>>> GetByBranchIdAndMode(
            long branchId,
            int mode,
            [FromQuery] string? search)
        {
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
        public async Task<ActionResult<PartnerViewDto>> Create([FromBody] PartnerCreateDto dto)
        {
            if (dto.BranchId <= 0)
            {
                return BadRequest(new { message = "BranchId là bắt buộc." });
            }

            try
            {
                var result = await _partnerService.CreatePartnerAsync(dto, 1); // Mock User ID 1 for now
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 4. Update (Chỉ dựa vào partnerId, không cập nhật BranchId & Type)
        [HttpPut("{id}")]
        public async Task<ActionResult<PartnerViewDto>> Update(long id, [FromBody] PartnerUpdateDto dto)
        {
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
        public async Task<ActionResult> Delete(long id)
        {
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
