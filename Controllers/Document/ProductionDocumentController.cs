using System;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Services.Document;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers.Document
{
    [ApiController]
    [Route("api/document")]
    public class ProductionDocumentController : ControllerBase
    {
        private readonly IProductionDocumentService _service;

        public ProductionDocumentController(IProductionDocumentService service)
        {
            _service = service;
        }

        private long? GetUserId()
        {
            var userIdStr = User.Identity?.IsAuthenticated == true
                ? User.FindFirst("AccountId")?.Value
                : null;
            return !string.IsNullOrEmpty(userIdStr) && long.TryParse(userIdStr, out var parsedId) && parsedId > 0
                ? parsedId
                : null;
        }

        // ==========================================
        // 6. PRODUCTION DOCUMENT (Sản xuất - Type 6)
        // ==========================================
        [HttpPost("production")]
        public async Task<IActionResult> CreateProduction([FromBody] ProductionDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = GetUserId();
                var result = await _service.CreateProductionDocumentAsync(dto, createdBy);
                
                return Ok(new
                {
                    message = "Tạo phiếu sản xuất thành công.",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo phiếu sản xuất.", error = ex.Message });
            }
        }

        [HttpDelete("production/{id}")]
        public async Task<IActionResult> DeleteProduction(long id)
        {
            try
            {
                var deletedBy = GetUserId();
                await _service.DeleteProductionDocumentAsync(id, deletedBy);
                return Ok(new { message = "Xóa phiếu sản xuất thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa phiếu sản xuất.", error = ex.Message });
            }
        }
    }
}
