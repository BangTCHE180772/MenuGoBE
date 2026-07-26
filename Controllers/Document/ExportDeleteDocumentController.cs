using System;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Services.Document;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers.Document
{
    [ApiController]
    [Route("api/document")]
    public class ExportDeleteDocumentController : ControllerBase
    {
        private readonly IExportDeleteDocumentService _service;

        public ExportDeleteDocumentController(IExportDeleteDocumentService service)
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
        // 2. EXPORT DELETE DOCUMENT (Xuất hủy - Type 2)
        // ==========================================
        [HttpPost("export-delete")]
        public async Task<IActionResult> CreateExportDelete([FromBody] ExportDeleteDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = GetUserId();
                var result = await _service.CreateExportDeleteDocumentAsync(dto, createdBy);
                
                return Ok(new
                {
                    message = "Tạo phiếu xuất hủy thành công.",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo phiếu xuất hủy.", error = ex.Message });
            }
        }

        [HttpDelete("export-delete/{id}")]
        public async Task<IActionResult> DeleteExportDelete(long id)
        {
            try
            {
                var deletedBy = GetUserId();
                await _service.DeleteExportDeleteDocumentAsync(id, deletedBy);
                return Ok(new { message = "Xóa phiếu xuất hủy thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa phiếu xuất hủy.", error = ex.Message });
            }
        }
    }
}
