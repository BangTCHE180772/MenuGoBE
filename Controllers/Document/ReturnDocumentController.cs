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
    public class ReturnDocumentController : ControllerBase
    {
        private readonly IReturnDocumentService _service;
        private readonly IDocumentService _documentService;

        public ReturnDocumentController(IReturnDocumentService service, IDocumentService documentService)
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
        // 4. RETURN DOCUMENT (Trả hàng nhập - Type 4)
        // ==========================================
        [HttpGet("return")]
        public async Task<IActionResult> GetReturnDocuments([FromQuery] long? branchId)
        {
            try
            {
                var result = await _documentService.GetDocumentsAsync(branchId, DocumentType.Return);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi lấy danh sách phiếu trả hàng nhập.", error = ex.Message });
            }
        }

        [HttpPost("return")]
        public async Task<IActionResult> CreateReturn([FromBody] ReturnDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = GetUserId();
                var result = await _service.CreateReturnDocumentAsync(dto, createdBy);
                
                return Ok(new
                {
                    message = "Tạo phiếu trả hàng nhập thành công.",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo phiếu trả hàng nhập.", error = ex.Message });
            }
        }

        [HttpDelete("return/{id}")]
        public async Task<IActionResult> DeleteReturn(long id)
        {
            try
            {
                var deletedBy = GetUserId();
                await _service.DeleteReturnDocumentAsync(id, deletedBy);
                return Ok(new { message = "Xóa phiếu trả hàng nhập thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa phiếu trả hàng nhập.", error = ex.Message });
            }
        }
    }
}
