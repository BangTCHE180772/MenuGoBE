using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Product;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/product/{productId:long}/recipes")]
    public class RecipeController : ControllerBase
    {
        private readonly IRecipeService _service;

        public RecipeController(IRecipeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(long productId)
        {
            var result = await _service.GetAllByProductAsync(productId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(long productId, [FromBody] ProductRecipeDto dto)
        {
            var result = await _service.CreateAsync(productId, dto);
            return Ok(result);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long productId, long id, [FromBody] ProductRecipeDto dto)
        {
            var result = await _service.UpdateAsync(productId, id, dto);
            return Ok(result);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long productId, long id)
        {
            var result = await _service.DeleteAsync(productId, id);
            if (!result)
            {
                return NotFound(new { message = "Không tìm thấy công thức cần xóa." });
            }
            return Ok(new { message = "Xóa công thức thành công." });
        }
    }
}
