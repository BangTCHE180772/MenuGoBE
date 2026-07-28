using System;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Services;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers.Document
{
    [ApiController]
    [Route("api/document")]
    public class ExportDeleteDocumentController : ControllerBase
    {
        private readonly IExportDeleteDocumentService _service;
        private readonly IDocumentService _documentService;

        public ExportDeleteDocumentController(IExportDeleteDocumentService service, IDocumentService documentService)
        {
            _service = service;
            _documentService = documentService;
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
        [HttpGet("export-delete")]
        public async Task<IActionResult> GetExportDeleteDocuments([FromQuery] long? branchId)
        {
            if (!branchId.HasValue || branchId.Value <= 0)
            {
                return BadRequest(new { message = "Vui lòng cung cấp branchId hợp lệ." });
            }

            try
            {
                var result = await _documentService.GetDocumentsAsync(branchId, DocumentType.Export);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi lấy danh sách phiếu xuất hủy.", error = ex.Message });
            }
        }

        [HttpPost("export-delete")]
        public async Task<IActionResult> CreateExportDelete([FromBody] ExportDeleteDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.CreateExportDeleteDocumentAsync(dto, dto.CreatedByAccountId);
                
                return Ok(new
                {
                    message = "Tạo phiếu xuất hủy thành công.",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (Exceptions.MenuGoException ex)
            {
                return StatusCode(ex.HttpStatusCode, new { code = ex.Code, message = ex.Message });
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
