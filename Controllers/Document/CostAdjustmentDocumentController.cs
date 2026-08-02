using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Controllers.Document
{
    /// <summary>
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Điều chỉnh giá vốn (Cost Adjustment Document).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CostAdjustmentDocumentController : BaseApiController
    {
        #region 0. Constructor & Dependency Injection
        private readonly IDocumentService _documentService;

        public CostAdjustmentDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }
        #endregion

        #region 1. Query APIs (Truy vấn Phiếu Điều chỉnh giá vốn)
        /// <summary>
        /// Lấy danh sách phiếu Điều chỉnh giá vốn theo Chi nhánh.
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/cost-adjustment")]
        public async Task<IActionResult> GetCostAdjustmentList([FromQuery] long branchId)
        {
            var result = await _documentService.GetCostAdjustmentListAsync(branchId);
            return Ok(result);
        }
        #endregion

        #region 2. Create APIs (Tạo mới Phiếu Điều chỉnh giá vốn: Nháp & Chốt)
        /// <summary>
        /// Tạo mới phiếu Điều chỉnh giá vốn ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("pending")]
        [HttpPost("/api/document/cost-adjustment/pending")]
        public async Task<IActionResult> CreateCostAdjustmentPending([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateCostAdjustmentPendingAsync(document, userId);
            return Ok(result);
        }

        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Điều chỉnh giá vốn sang Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/cost-adjustment/completed")]
        public async Task<IActionResult> CreateCostAdjustmentCompleted([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateCostAdjustmentCompletedAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region 3. Update & State Transition APIs (Cập nhật & Chốt phiếu)
        /// <summary>
        /// Cập nhật phiếu Điều chỉnh giá vốn ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPut("pending/{id}")]
        [HttpPut("/api/document/cost-adjustment/pending/{id}")]
        public async Task<IActionResult> UpdateCostAdjustmentPending(long id, [FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.UpdateCostAdjustmentPendingAsync(id, document, userId);
            return Ok(result);
        }

        /// <summary>
        /// Chốt phiếu Điều chỉnh giá vốn Pending -> Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("{id}/complete")]
        [HttpPost("/api/document/cost-adjustment/{id}/complete")]
        public async Task<IActionResult> CompleteCostAdjustment(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CompleteCostAdjustmentAsync(id, userId);
            return Ok(result);
        }
        #endregion

        #region 4. Soft Delete APIs (Hủy Phiếu Điều chỉnh giá vốn)
        /// <summary>
        /// Xóa mềm phiếu Điều chỉnh giá vốn Pending -> Chuyển sang Canceled.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpDelete("{id}/soft-delete")]
        [HttpDelete("/api/document/cost-adjustment/{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteCostAdjustmentPending(long id, [FromQuery] string deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var success = await _documentService.SoftDeleteCostAdjustmentPendingAsync(id, userId, deleteNote);
            return Ok(new { success, message = "Đã chuyển phiếu Điều chỉnh giá vốn sang trạng thái Canceled." });
        }
        #endregion
    }
}
