using System;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Services.Document;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers.Document
{
    [ApiController]
    [Route("api/document")]
    public class CheckDocumentController : ControllerBase
    {
        private readonly ICheckDocumentService _service;

        public CheckDocumentController(ICheckDocumentService service)
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
        // 5. CHECK DOCUMENT (Kiểm kho - Type 5)
        // ==========================================
        [HttpPost("check")]
        public async Task<IActionResult> CreateCheck([FromBody] CheckDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = GetUserId();
                var result = await _service.CreateCheckDocumentAsync(dto, createdBy);
                
                return Ok(new
                {
                    message = "Tạo phiếu kiểm kho thành công.",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo phiếu kiểm kho.", error = ex.Message });
            }
        }

        [HttpPost("adjustment")]
        public async Task<IActionResult> CreateAdjustment([FromBody] AdjustmentDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = GetUserId();
                var result = await _service.CreateAdjustmentDocumentAsync(dto, createdBy);
                
                return Ok(new
                {
                    message = "Tạo phiếu kiểm kho thành công.",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo phiếu kiểm kho.", error = ex.Message });
            }
        }

        [HttpDelete("check/{id}")]
        [HttpDelete("adjustment/{id}")]
        public async Task<IActionResult> DeleteCheck(long id)
        {
            try
            {
                var deletedBy = GetUserId();
                await _service.DeleteCheckDocumentAsync(id, deletedBy);
                return Ok(new { message = "Xóa phiếu kiểm kho thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa phiếu kiểm kho.", error = ex.Message });
            }
        }
    }
}
