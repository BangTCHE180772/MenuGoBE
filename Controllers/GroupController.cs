using MenuGoBE.Dtos.Group;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _service;

        public GroupController(IGroupService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("sellable")]
        public async Task<IActionResult> GetSellable([FromQuery] long? branchId = null)
        {
            var result = await _service.GetSellableAsync(branchId);
            return Ok(result);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy Group." });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GroupCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] GroupUpdateDto dto)
        {
            var result = await _service.UpdateAsync(dto);
            if (!result)
                return NotFound(new { message = "Không tìm thấy Group để cập nhật." });
            return Ok(new { message = "Cập nhật Group thành công." });
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = "Không tìm thấy Group để xóa." });
            return Ok(new { message = "Xóa Group thành công." });
        }
    }
}
