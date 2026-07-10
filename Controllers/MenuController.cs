using MenuGoBE.Dtos.Menu;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : ControllerBase
    {
        private readonly IMenuService _service;

        public MenuController(IMenuService service)
        {
            _service = service;
        }

        // ── CRUD ─────────────────────────────────────────────────────────────

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
                return NotFound(new { message = "Không tìm thấy Menu." });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MenuCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] MenuUpdateDto dto)
        {
            var result = await _service.UpdateAsync(dto);
            if (!result)
                return NotFound(new { message = "Không tìm thấy Menu để cập nhật." });
            return Ok(new { message = "Cập nhật Menu thành công." });
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = "Không tìm thấy Menu để xóa." });
            return Ok(new { message = "Xóa Menu thành công." });
        }

        // ── Single product ────────────────────────────────────────────────────

        [HttpGet("{id:long}/products")]
        public async Task<IActionResult> GetProducts(long id)
        {
            var result = await _service.GetProductsAsync(id);
            return Ok(result);
        }

        /// <summary>Thêm 1 product vào menu.</summary>
        [HttpPost("products/add")]
        public async Task<IActionResult> AddProduct([FromBody] MenuProductDto dto)
        {
            var (success, message) = await _service.AddProductAsync(dto);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        /// <summary>Xóa 1 product khỏi menu.</summary>
        [HttpDelete("products/remove")]
        public async Task<IActionResult> RemoveProduct([FromBody] MenuProductDto dto)
        {
            var (success, message) = await _service.RemoveProductAsync(dto);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        // ── Bulk products ──────────────────────────────────────────────────────

        /// <summary>Thêm nhiều product vào menu cùng lúc.</summary>
        [HttpPost("products/add-bulk")]
        public async Task<IActionResult> AddProducts([FromBody] MenuProductsBulkDto dto)
        {
            var (success, message) = await _service.AddProductsAsync(dto);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        /// <summary>Xóa nhiều product khỏi menu cùng lúc.</summary>
        [HttpDelete("products/remove-bulk")]
        public async Task<IActionResult> RemoveProducts([FromBody] MenuProductsBulkDto dto)
        {
            var (success, message) = await _service.RemoveProductsAsync(dto);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }
    }
}
