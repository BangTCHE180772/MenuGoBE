using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Controllers.Document
{
    /// <summary>
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Sản xuất (Production Document).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductionDocumentController : BaseApiController
    {
        #region 0. Constructor & Dependency Injection
        private readonly IDocumentService _documentService;

        public ProductionDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }
        #endregion

        #region 1. Query APIs (Truy vấn Phiếu Sản xuất)
        /// <summary>
        /// Lấy danh sách phiếu Sản xuất theo Chi nhánh.
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/production")]
        public async Task<IActionResult> GetProductionList([FromQuery] long branchId)
        {
            var result = await _documentService.GetProductionListAsync(branchId);
            return Ok(result);
        }
        #endregion

        #region 2. Create APIs (Tạo mới Phiếu Sản xuất: Nháp & Chốt)
        /// <summary>
        /// Tạo mới phiếu Sản xuất ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("pending")]
        [HttpPost("/api/document/production/pending")]
        public async Task<IActionResult> CreateProductionPending([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateProductionPendingAsync(document, userId);
            return Ok(result);
        }

        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Sản xuất sang Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/production/completed")]
        public async Task<IActionResult> CreateProductionCompleted([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateProductionCompletedAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region 3. Update & State Transition APIs (Cập nhật & Chốt phiếu)
        /// <summary>
        /// Cập nhật phiếu Sản xuất ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPut("pending/{id}")]
        [HttpPut("/api/document/production/pending/{id}")]
        public async Task<IActionResult> UpdateProductionPending(long id, [FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.UpdateProductionPendingAsync(id, document, userId);
            return Ok(result);
        }

        /// <summary>
        /// Chốt phiếu Sản xuất Pending -> Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("{id}/complete")]
        [HttpPost("/api/document/production/{id}/complete")]
        public async Task<IActionResult> CompleteProduction(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CompleteProductionAsync(id, userId);
            return Ok(result);
        }
        #endregion

        #region 4. Soft Delete APIs (Hủy Phiếu Sản xuất)
        /// <summary>
        /// Xóa mềm phiếu Sản xuất Pending -> Chuyển sang Canceled.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpDelete("{id}/soft-delete")]
        [HttpDelete("/api/document/production/{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteProductionPending(long id, [FromQuery] string deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var success = await _documentService.SoftDeleteProductionPendingAsync(id, userId, deleteNote);
            return Ok(new { success, message = "Đã chuyển phiếu Sản xuất sang trạng thái Canceled." });
        }
        #endregion
    }
}
