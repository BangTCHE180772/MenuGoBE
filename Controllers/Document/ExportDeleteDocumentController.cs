using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Controllers.Document
{
    /// <summary>
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Xuất hủy (Export Delete / Waste Document).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ExportDeleteDocumentController : BaseApiController
    {
        #region 0. Constructor & Dependency Injection
        private readonly IDocumentService _documentService;

        public ExportDeleteDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }
        #endregion

        #region 1. Query APIs (Truy vấn Phiếu Xuất hủy)
        /// <summary>
        /// Lấy danh sách phiếu Xuất hủy theo Chi nhánh.
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/export-delete")]
        public async Task<IActionResult> GetExportDeleteList([FromQuery] long branchId)
        {
            var result = await _documentService.GetExportDeleteListAsync(branchId);
            return Ok(result);
        }
        #endregion

        #region 2. Create APIs (Tạo mới Phiếu Xuất hủy: Nháp & Chốt)
        /// <summary>
        /// Tạo mới phiếu Xuất hủy ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("pending")]
        [HttpPost("/api/document/export-delete/pending")]
        public async Task<IActionResult> CreateExportDeletePending([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateExportDeletePendingAsync(document, userId);
            return Ok(result);
        }

        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Xuất hủy sang Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/export-delete/completed")]
        public async Task<IActionResult> CreateExportDeleteCompleted([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateExportDeleteCompletedAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region 3. Update & State Transition APIs (Cập nhật & Chốt phiếu)
        /// <summary>
        /// Cập nhật phiếu Xuất hủy ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPut("pending/{id}")]
        [HttpPut("/api/document/export-delete/pending/{id}")]
        public async Task<IActionResult> UpdateExportDeletePending(long id, [FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.UpdateExportDeletePendingAsync(id, document, userId);
            return Ok(result);
        }

        /// <summary>
        /// Chốt phiếu Xuất hủy Pending -> Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("{id}/complete")]
        [HttpPost("/api/document/export-delete/{id}/complete")]
        public async Task<IActionResult> CompleteExportDelete(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CompleteExportDeleteAsync(id, userId);
            return Ok(result);
        }
        #endregion

        #region 4. Soft Delete APIs (Hủy Phiếu Xuất hủy)
        /// <summary>
        /// Xóa mềm phiếu Xuất hủy Pending -> Chuyển sang Canceled.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpDelete("{id}/soft-delete")]
        [HttpDelete("/api/document/export-delete/{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteExportDeletePending(long id, [FromQuery] string deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var success = await _documentService.SoftDeleteExportDeletePendingAsync(id, userId, deleteNote);
            return Ok(new { success, message = "Đã chuyển phiếu Xuất hủy sang trạng thái Canceled." });
        }
        #endregion
    }
}
