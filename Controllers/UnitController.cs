using MenuGoBE.Dtos.Unit;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UnitController : ControllerBase
    {
        private readonly IUnitService _service;

        public UnitController(IUnitService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy Đơn vị." });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UnitCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UnitUpdateDto dto)
        {
            var result = await _service.UpdateAsync(dto);
            if (!result)
                return NotFound(new { message = "Không tìm thấy Đơn vị để cập nhật." });
            return Ok(new { message = "Cập nhật Đơn vị thành công." });
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = "Không tìm thấy Đơn vị để xóa." });
            return Ok(new { message = "Xóa Đơn vị thành công." });
        }
    }
}
