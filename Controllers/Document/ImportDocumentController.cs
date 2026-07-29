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
    public class ImportDocumentController : ControllerBase
    {
        private readonly IImportDocumentService _service;
        private readonly IDocumentService _documentService;

        public ImportDocumentController(IImportDocumentService service, IDocumentService documentService)
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
        // 1. IMPORT DOCUMENT (Nhập hàng - Type 1)
        // ==========================================
        [HttpGet("import")]
        public async Task<IActionResult> GetImportDocuments([FromQuery] long? branchId)
        {
            if (!branchId.HasValue || branchId.Value <= 0)
            {
                return BadRequest(new { message = "Vui lòng cung cấp branchId hợp lệ." });
            }

            try
            {
                var result = await _documentService.GetDocumentsAsync(branchId, DocumentType.Import);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi lấy danh sách phiếu nhập kho.", error = ex.Message });
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> CreateImport([FromBody] ImportDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.CreateImportDocumentAsync(dto, dto.CreatedByAccountId);
                
                return Ok(new
                {
                    message = "Tạo phiếu nhập kho thành công.",
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

        [HttpGet("import/{id}/return-details")]
        public async Task<IActionResult> GetImportReturnDetails(long id)
        {
            try
            {
                var result = await _service.GetImportReturnDetailsAsync(id);
                return Ok(result);
            }
            catch (Exceptions.MenuGoException ex)
            {
                return StatusCode(ex.HttpStatusCode, new { code = ex.Code, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi lấy chi tiết trả hàng nhập.", error = ex.Message });
            }
        }
    }
}
