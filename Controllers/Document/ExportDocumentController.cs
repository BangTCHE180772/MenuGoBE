using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Controllers.Document
{
    /// <summary>
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Xuất dùng nội bộ (Export Document).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ExportDocumentController : BaseApiController
    {
        #region 0. Constructor & Dependency Injection
        private readonly IDocumentService _documentService;

        public ExportDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }
        #endregion

        #region 1. Query APIs (Truy vấn Phiếu Xuất dùng nội bộ)
        /// <summary>
        /// Lấy danh sách phiếu Xuất dùng nội bộ theo Chi nhánh.
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/export")]
        public async Task<IActionResult> GetExportList([FromQuery] long branchId)
        {
            var result = await _documentService.GetExportListAsync(branchId);
            return Ok(result);
        }
        #endregion

        #region 2. Create APIs (Tạo mới Phiếu Xuất dùng nội bộ)
        /// <summary>
        /// Tạo mới phiếu Xuất dùng nội bộ ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("pending")]
        [HttpPost("/api/document/export/pending")]
        public async Task<IActionResult> CreateExportPending([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateExportPendingAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region 3. Update & State Transition APIs (Cập nhật & Chốt phiếu)
        /// <summary>
        /// Cập nhật phiếu Xuất dùng nội bộ ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPut("pending/{id}")]
        [HttpPut("/api/document/export/pending/{id}")]
        public async Task<IActionResult> UpdateExportPending(long id, [FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.UpdateExportPendingAsync(id, document, userId);
            return Ok(result);
        }

        /// <summary>
        /// Chốt phiếu Xuất dùng nội bộ Pending -> Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("{id}/complete")]
        [HttpPost("/api/document/export/{id}/complete")]
        public async Task<IActionResult> CompleteExport(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CompleteExportAsync(id, userId);
            return Ok(result);
        }
        #endregion

        #region 4. Soft Delete APIs (Hủy Phiếu Xuất dùng nội bộ)
        /// <summary>
        /// Xóa mềm phiếu Xuất dùng nội bộ Pending -> Chuyển sang Canceled.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpDelete("{id}/soft-delete")]
        [HttpDelete("/api/document/export/{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteExportPending(long id, [FromQuery] string deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var success = await _documentService.SoftDeleteExportPendingAsync(id, userId, deleteNote);
            return Ok(new { success, message = "Đã chuyển phiếu Xuất dùng nội bộ sang trạng thái Canceled." });
        }
        #endregion
    }
}
