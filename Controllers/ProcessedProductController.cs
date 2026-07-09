using MenuGoBE.Dtos.Product;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProcessedProductController : ControllerBase
    {
        private readonly IProductService _service;
        private const string ProductType = "processed";

        public ProcessedProductController(IProductService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetByTypeAsync(ProductType);
            return Ok(result);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAndTypeAsync(id, ProductType);
            if (result == null)
                return NotFound(new { message = $"Không tìm thấy sản phẩm {ProductType}." });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
        {
            var result = await _service.CreateWithTypeAsync(dto, ProductType);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ProductUpdateDto dto)
        {
            var result = await _service.UpdateWithTypeAsync(dto, ProductType);
            if (!result)
                return NotFound(new { message = $"Không tìm thấy sản phẩm {ProductType} để cập nhật." });
            return Ok(new { message = $"Cập nhật sản phẩm {ProductType} thành công." });
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteWithTypeAsync(id, ProductType);
            if (!result)
                return NotFound(new { message = $"Không tìm thấy sản phẩm {ProductType} để xóa." });
            return Ok(new { message = $"Xóa sản phẩm {ProductType} thành công." });
        }
    }
}
