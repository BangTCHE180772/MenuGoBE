using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Controllers.Document
{
    /// <summary>
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Kiểm kho (Inventory Check Document).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CheckDocumentController : BaseApiController
    {
        #region 0. Constructor & Dependency Injection
        private readonly IDocumentService _documentService;

        public CheckDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }
        #endregion

        #region 1. Query APIs (Truy vấn Phiếu Kiểm kho)
        /// <summary>
        /// Lấy danh sách phiếu Kiểm kho theo Chi nhánh.
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/check")]
        public async Task<IActionResult> GetCheckList([FromQuery] long branchId)
        {
            var result = await _documentService.GetCheckListAsync(branchId);
            return Ok(result);
        }
        #endregion

        #region 2. Create APIs (Tạo mới Phiếu Kiểm kho: Nháp & Chốt)
        /// <summary>
        /// Tạo mới phiếu Kiểm kho ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("pending")]
        [HttpPost("/api/document/check/pending")]
        public async Task<IActionResult> CreateCheckPending([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateCheckPendingAsync(document, userId);
            return Ok(result);
        }

        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Kiểm kho sang Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/check/completed")]
        public async Task<IActionResult> CreateCheckCompleted([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateCheckCompletedAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region 3. Update & State Transition APIs (Cập nhật & Chốt phiếu)
        /// <summary>
        /// Cập nhật phiếu Kiểm kho ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPut("pending/{id}")]
        [HttpPut("/api/document/check/pending/{id}")]
        public async Task<IActionResult> UpdateCheckPending(long id, [FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.UpdateCheckPendingAsync(id, document, userId);
            return Ok(result);
        }

        /// <summary>
        /// Chốt phiếu Kiểm kho Pending -> Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("{id}/complete")]
        [HttpPost("/api/document/check/{id}/complete")]
        public async Task<IActionResult> CompleteCheck(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CompleteCheckAsync(id, userId);
            return Ok(result);
        }
        #endregion

        #region 4. Soft Delete APIs (Hủy Phiếu Kiểm kho)
        /// <summary>
        /// Xóa mềm phiếu Kiểm kho Pending -> Chuyển sang Canceled.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpDelete("{id}/soft-delete")]
        [HttpDelete("/api/document/check/{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteCheckPending(long id, [FromQuery] string deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var success = await _documentService.SoftDeleteCheckPendingAsync(id, userId, deleteNote);
            return Ok(new { success, message = "Đã chuyển phiếu Kiểm kho sang trạng thái Canceled." });
        }
        #endregion
    }
}
