using System;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Services.Document;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers.Document
{
    [ApiController]
    [Route("api/document")]
    public class ImportDocumentController : ControllerBase
    {
        private readonly IImportDocumentService _service;

        public ImportDocumentController(IImportDocumentService service)
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
        // 1. IMPORT DOCUMENT (Nhập hàng - Type 1)
        // ==========================================
        [HttpPost("import")]
        public async Task<IActionResult> CreateImport([FromBody] ImportDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = GetUserId();
                var result = await _service.CreateImportDocumentAsync(dto, createdBy);
                
                return Ok(new
                {
                    message = "Tạo phiếu nhập kho thành công.",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo phiếu nhập kho.", error = ex.Message });
            }
        }

        [HttpDelete("import/{id}")]
        public async Task<IActionResult> DeleteImport(long id)
        {
            try
            {
                var deletedBy = GetUserId();
                await _service.DeleteImportDocumentAsync(id, deletedBy);
                return Ok(new { message = "Xóa phiếu nhập kho thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa phiếu nhập kho.", error = ex.Message });
            }
        }
    }
}
