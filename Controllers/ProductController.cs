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
        /// Create a new product (general)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }

        [HttpPost("/api/ProcessedProduct")]
        public async Task<IActionResult> CreateProcessed([FromBody] ProcessedProductCreateDto dto)
        {
            var result = await _service.CreateProcessedAsync(dto);
            return Ok(result);
        }

        [HttpPost("/api/ManufacturedProduct")]
        public async Task<IActionResult> CreateManufactured([FromBody] ManufacturedProductCreateDto dto)
        {
            var result = await _service.CreateManufacturedAsync(dto);
            return Ok(result);
        }

        [HttpPost("/api/RegularProduct")]
        public async Task<IActionResult> CreateRegular([FromBody] RegularProductCreateDto dto)
        {
            var result = await _service.CreateRegularAsync(dto);
            return Ok(result);
        }

        [HttpPost("/api/IngredientProduct")]
        public async Task<IActionResult> CreateIngredient([FromBody] IngredientProductCreateDto dto)
        {
            var result = await _service.CreateIngredientAsync(dto);
            return Ok(result);
        }

        [HttpPost("/api/ToolProduct")]
        public async Task<IActionResult> CreateTool([FromBody] ToolProductCreateDto dto)
        {
            var result = await _service.CreateToolAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Update an existing product (general)
        /// </summary>
        [HttpPut]
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update([FromBody] ProductUpdateDto dto, long? id = null)
        {
            if (id.HasValue && id.Value > 0)
            {
                dto.Id = id.Value;
            }

            var result = await _service.UpdateAsync(dto);
            if (!result)
                return NotFound(new { message = "Không tìm thấy sản phẩm để cập nhật." });

            return Ok(new { message = "Cập nhật sản phẩm thành công." });
        }

        /// <summary>
        /// Cập nhật giá bán hàng loạt (chỉ Regular, Manufactured, Processed).
        /// Body: [{ "id": 1, "sellPrice": 50000 }, ...]
        /// </summary>
        [HttpPatch("sell-price")]
        public async Task<IActionResult> BulkUpdateSellPrice([FromBody] List<SellPriceBulkUpdateItemDto> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest(new { message = "Danh sách sản phẩm cần cập nhật không được rỗng." });

            var count = await _service.BulkUpdateSellPriceAsync(items);
            return Ok(new { message = $"Đã cập nhật giá bán cho {count} sản phẩm.", updatedCount = count });
        }

        [HttpPut("/api/ProcessedProduct")]
        [HttpPut("/api/ProcessedProduct/{id:long}")]
        public async Task<IActionResult> UpdateProcessed([FromBody] ProcessedProductUpdateDto dto, long? id = null)
        {
            if (id.HasValue && id.Value > 0) dto.Id = id.Value;
            var result = await _service.UpdateProcessedAsync(dto);
            if (!result) return NotFound(new { message = "Không tìm thấy sản phẩm để cập nhật." });
            return Ok(new { message = "Cập nhật sản phẩm thành công." });
        }

        [HttpPut("/api/ManufacturedProduct")]
        [HttpPut("/api/ManufacturedProduct/{id:long}")]
        public async Task<IActionResult> UpdateManufactured([FromBody] ManufacturedProductUpdateDto dto, long? id = null)
        {
            if (id.HasValue && id.Value > 0) dto.Id = id.Value;
            var result = await _service.UpdateManufacturedAsync(dto);
            if (!result) return NotFound(new { message = "Không tìm thấy sản phẩm để cập nhật." });
            return Ok(new { message = "Cập nhật sản phẩm thành công." });
        }

        [HttpPut("/api/RegularProduct")]
        [HttpPut("/api/RegularProduct/{id:long}")]
        public async Task<IActionResult> UpdateRegular([FromBody] RegularProductUpdateDto dto, long? id = null)
        {
            if (id.HasValue && id.Value > 0) dto.Id = id.Value;
            var result = await _service.UpdateRegularAsync(dto);
            if (!result) return NotFound(new { message = "Không tìm thấy sản phẩm để cập nhật." });
            return Ok(new { message = "Cập nhật sản phẩm thành công." });
        }

        [HttpPut("/api/IngredientProduct")]
        [HttpPut("/api/IngredientProduct/{id:long}")]
        public async Task<IActionResult> UpdateIngredient([FromBody] IngredientProductUpdateDto dto, long? id = null)
        {
            if (id.HasValue && id.Value > 0) dto.Id = id.Value;
            var result = await _service.UpdateIngredientAsync(dto);
            if (!result) return NotFound(new { message = "Không tìm thấy sản phẩm để cập nhật." });
            return Ok(new { message = "Cập nhật sản phẩm thành công." });
        }

        [HttpPut("/api/ToolProduct")]
        [HttpPut("/api/ToolProduct/{id:long}")]
        public async Task<IActionResult> UpdateTool([FromBody] ToolProductUpdateDto dto, long? id = null)
        {
            if (id.HasValue && id.Value > 0) dto.Id = id.Value;
            var result = await _service.UpdateToolAsync(dto);
            if (!result) return NotFound(new { message = "Không tìm thấy sản phẩm để cập nhật." });
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
