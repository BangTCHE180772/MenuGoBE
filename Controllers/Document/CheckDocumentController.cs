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
    public class CheckDocumentController : ControllerBase
    {
        private readonly ICheckDocumentService _service;
        private readonly IDocumentService _documentService;

        public CheckDocumentController(ICheckDocumentService service, IDocumentService documentService)
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
        // 5. CHECK DOCUMENT (Kiểm kho - Type 5)
        // ==========================================
        [HttpGet("check")]
        public async Task<IActionResult> GetCheckDocuments([FromQuery] long? branchId)
        {
            if (!branchId.HasValue || branchId.Value <= 0)
            {
                return BadRequest(new { message = "Vui lòng cung cấp branchId hợp lệ." });
            }

            try
            {
                var result = await _documentService.GetDocumentsAsync(branchId, DocumentType.Check);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi lấy danh sách phiếu kiểm kho.", error = ex.Message });
            }
        }

        [HttpPost("check")]
        public async Task<IActionResult> CreateCheck([FromBody] CheckDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.CreateCheckDocumentAsync(dto, dto.CreatedByAccountId);
                
                return Ok(new
                {
                    message = "Tạo phiếu kiểm kho thành công.",
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
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo phiếu kiểm kho.", error = ex.Message });
            }
        }

        [HttpDelete("check/{id}")]
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

