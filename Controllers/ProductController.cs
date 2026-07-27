using MenuGoBE.Dtos.Product;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductController(IProductService service)
        {
            _service = service;
        }

        /// <summary>
        /// Get all products, optionally filtered by ProductType enum (e.g. ?type=Processed or ?type=1)
        /// Also supports legacy alias routes: /api/ProcessedProduct, /api/ManufacturedProduct, etc.
        /// </summary>
        [HttpGet]
        [HttpGet("/api/ProcessedProduct")]
        [HttpGet("/api/ManufacturedProduct")]
        [HttpGet("/api/RegularProduct")]
        [HttpGet("/api/IngredientProduct")]
        [HttpGet("/api/ToolProduct")]
        public async Task<IActionResult> GetAll([FromQuery] ProductType? type)
        {
            // If called from a legacy alias route, infer type from path
            var path = HttpContext.Request.Path.Value?.ToLower() ?? "";
            if (!type.HasValue)
            {
                if (path.Contains("processedproduct")) type = ProductType.Processed;
                else if (path.Contains("manufacturedproduct")) type = ProductType.Manufactured;
                else if (path.Contains("regularproduct")) type = ProductType.Regular;
                else if (path.Contains("ingredientproduct")) type = ProductType.Ingredient;
                else if (path.Contains("toolproduct")) type = ProductType.Tool;
            }

            var result = await _service.GetAllAsync(type);
            return Ok(result);
        }

        /// <summary>
        /// Get product by ID (with optional type validation)
        /// </summary>
        [HttpGet("{id:long}")]
        [HttpGet("/api/ProcessedProduct/{id:long}")]
        [HttpGet("/api/ManufacturedProduct/{id:long}")]
        [HttpGet("/api/RegularProduct/{id:long}")]
        [HttpGet("/api/IngredientProduct/{id:long}")]
        [HttpGet("/api/ToolProduct/{id:long}")]
        public async Task<IActionResult> GetById(long id, [FromQuery] ProductType? type)
        {
            var path = HttpContext.Request.Path.Value?.ToLower() ?? "";
            if (!type.HasValue)
            {
                if (path.Contains("processedproduct")) type = ProductType.Processed;
                else if (path.Contains("manufacturedproduct")) type = ProductType.Manufactured;
                else if (path.Contains("regularproduct")) type = ProductType.Regular;
                else if (path.Contains("ingredientproduct")) type = ProductType.Ingredient;
                else if (path.Contains("toolproduct")) type = ProductType.Tool;
            }

            ProductViewDto? result;
            if (type.HasValue)
            {
                result = await _service.GetByIdAndTypeAsync(id, type.Value);
            }
            else
            {
                result = await _service.GetByIdAsync(id);
            }

            if (result == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm." });

            return Ok(result);
        }

        /// <summary>
        /// Create a new product
        /// </summary>
        [HttpPost]
        [HttpPost("/api/ProcessedProduct")]
        [HttpPost("/api/ManufacturedProduct")]
        [HttpPost("/api/RegularProduct")]
        [HttpPost("/api/IngredientProduct")]
        [HttpPost("/api/ToolProduct")]
        public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
        {
            var path = HttpContext.Request.Path.Value?.ToLower() ?? "";
            ProductType? overrideType = null;
            if (path.Contains("processedproduct")) overrideType = ProductType.Processed;
            else if (path.Contains("manufacturedproduct")) overrideType = ProductType.Manufactured;
            else if (path.Contains("regularproduct")) overrideType = ProductType.Regular;
            else if (path.Contains("ingredientproduct")) overrideType = ProductType.Ingredient;
            else if (path.Contains("toolproduct")) overrideType = ProductType.Tool;

            var result = await _service.CreateAsync(dto, overrideType);
            return Ok(result);
        }

        /// <summary>
        /// Update an existing product
        /// </summary>
        [HttpPut]
        [HttpPut("{id:long}")]
        [HttpPut("/api/ProcessedProduct")]
        [HttpPut("/api/ManufacturedProduct")]
        [HttpPut("/api/RegularProduct")]
        [HttpPut("/api/IngredientProduct")]
        [HttpPut("/api/ToolProduct")]
        public async Task<IActionResult> Update([FromBody] ProductUpdateDto dto, long? id = null)
        {
            if (id.HasValue && id.Value > 0)
            {
                dto.Id = id.Value;
            }

            var path = HttpContext.Request.Path.Value?.ToLower() ?? "";
            ProductType? overrideType = null;
            if (path.Contains("processedproduct")) overrideType = ProductType.Processed;
            else if (path.Contains("manufacturedproduct")) overrideType = ProductType.Manufactured;
            else if (path.Contains("regularproduct")) overrideType = ProductType.Regular;
            else if (path.Contains("ingredientproduct")) overrideType = ProductType.Ingredient;
            else if (path.Contains("toolproduct")) overrideType = ProductType.Tool;

            var result = await _service.UpdateAsync(dto, overrideType);
            if (!result)
                return NotFound(new { message = "Không tìm thấy sản phẩm để cập nhật." });

            return Ok(new { message = "Cập nhật sản phẩm thành công." });
        }

        /// <summary>
        /// Delete a product by ID
        /// </summary>
        [HttpDelete("{id:long}")]
        [HttpDelete("/api/ProcessedProduct/{id:long}")]
        [HttpDelete("/api/ManufacturedProduct/{id:long}")]
        [HttpDelete("/api/RegularProduct/{id:long}")]
        [HttpDelete("/api/IngredientProduct/{id:long}")]
        [HttpDelete("/api/ToolProduct/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var path = HttpContext.Request.Path.Value?.ToLower() ?? "";
            ProductType? expectedType = null;
            if (path.Contains("processedproduct")) expectedType = ProductType.Processed;
            else if (path.Contains("manufacturedproduct")) expectedType = ProductType.Manufactured;
            else if (path.Contains("regularproduct")) expectedType = ProductType.Regular;
            else if (path.Contains("ingredientproduct")) expectedType = ProductType.Ingredient;
            else if (path.Contains("toolproduct")) expectedType = ProductType.Tool;

            var result = await _service.DeleteAsync(id, expectedType);
            if (!result)
                return NotFound(new { message = "Không tìm thấy sản phẩm để xóa." });

            return Ok(new { message = "Xóa sản phẩm thành công." });
        }
    }
}
