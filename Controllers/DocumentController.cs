using System;
using System.Threading.Tasks;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _service;

        public DocumentController(IDocumentService service)
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
        // GET DOCUMENTS BY BRANCH AND/OR TYPE
        // GET /api/document?branchId=1&type=1
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetDocuments([FromQuery] long? branchId, [FromQuery] DocumentType? type)
        {
            if (!branchId.HasValue || branchId.Value <= 0)
            {
                return BadRequest(new { message = "Vui lòng cung cấp branchId hợp lệ." });
            }

            try
            {
                var result = await _service.GetDocumentsAsync(branchId, type);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi lấy danh sách chứng từ.", error = ex.Message });
            }
        }

        // ==========================================
        // GET DOCUMENT BY ID
        // GET /api/document/101
        // ==========================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocumentById(long id)
        {
            try
            {
                var result = await _service.GetDocumentByIdAsync(id);
                if (result == null)
                    return NotFound(new { message = "Không tìm thấy chứng từ." });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi lấy chi tiết chứng từ.", error = ex.Message });
            }
        }

        // ==========================================
        // GENERIC DELETE ENDPOINT
        // ==========================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(long id)
        {
            try
            {
                var deletedBy = GetUserId();
                await _service.DeleteDocumentAsync(id, deletedBy);
                return Ok(new { message = "Xóa chứng từ thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa chứng từ.", error = ex.Message });
            }
        }
    }
}
