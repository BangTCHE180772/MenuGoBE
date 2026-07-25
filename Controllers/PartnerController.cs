using MenuGoBE.Dtos.Base;
using MenuGoBE.Dtos.Partner;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet]
        public async Task<ActionResult<PagedResult<PartnerViewDto>>> GetPartners([FromQuery] PartnerQueryDto query)
        {
            var result = await _partnerService.GetPagedPartnersAsync(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PartnerViewDto>> GetPartnerById(long id)
        {
            try
            {
                var result = await _partnerService.GetPartnerByIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<PartnerViewDto>> CreatePartner([FromBody] PartnerCreateDto dto)
        {
            try
            {
                var result = await _partnerService.CreatePartnerAsync(dto, 1); // Mock User ID 1 for now
                return CreatedAtAction(nameof(GetPartnerById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<PartnerViewDto>> UpdatePartner(long id, [FromBody] PartnerUpdateDto dto)
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

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePartner(long id)
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
