using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Controllers.Document
{
    /// <summary>
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Chuyển kho (Transfer Document).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TransferDocumentController : BaseApiController
    {
        #region 0. Constructor & Dependency Injection
        private readonly IDocumentService _documentService;

        public TransferDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }
        #endregion

        #region 1. Query APIs (Truy vấn Phiếu Chuyển kho)
        /// <summary>
        /// Lấy danh sách phiếu Chuyển kho theo Chi nhánh gửi.
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/transfer")]
        public async Task<IActionResult> GetTransferList([FromQuery] long branchId)
        {
            var result = await _documentService.GetTransferListAsync(branchId);
            return Ok(result);
        }
        #endregion

        #region 2. Create APIs (Tạo mới Phiếu Chuyển kho: Nháp & Chốt)
        /// <summary>
        /// Tạo mới phiếu Chuyển kho ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("pending")]
        [HttpPost("/api/document/transfer/pending")]
        public async Task<IActionResult> CreateTransferPending([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateTransferPendingAsync(document, userId);
            return Ok(result);
        }

        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Chuyển kho sang Completed tại Chi nhánh Gửi.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/transfer/completed")]
        public async Task<IActionResult> CreateTransferCompleted([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateTransferCompletedAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region 3. Update & State Transition APIs (Cập nhật & Xử lý Chuyển/Nhận hàng)
        /// <summary>
        /// Cập nhật phiếu Chuyển kho ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPut("pending/{id}")]
        [HttpPut("/api/document/transfer/pending/{id}")]
        public async Task<IActionResult> UpdateTransferPending(long id, [FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.UpdateTransferPendingAsync(id, document, userId);
            return Ok(result);
        }

        /// <summary>
        /// Chốt phiếu Chuyển kho Pending -> Completed tại Chi nhánh Gửi.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("{id}/complete")]
        [HttpPost("/api/document/transfer/{id}/complete")]
        public async Task<IActionResult> CompleteTransfer(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CompleteTransferAsync(id, userId);
            return Ok(result);
        }

        /// <summary>
        /// Xử lý nhận hàng / từ chối nhận hàng tại Chi nhánh Nhận khi phiếu đang ở trạng thái InTransit.
        /// Chấp nhận 3 trạng thái: Received, PartialReceived, Rejected.
        /// </summary>
        [HttpPost("{id}/receive")]
        [HttpPost("/api/document/transfer/{id}/receive")]
        public async Task<IActionResult> ProcessTransferReceipt(long id, [FromBody] TransferReceiptRequest request)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            if (request == null)
            {
                return BadRequest(new { message = "Thông tin xác nhận nhận hàng không được để trống." });
            }

            var result = await _documentService.ProcessTransferReceiptAsync(id, request.Status, request.ReceivedDetails, request.Note, userId);
            return Ok(result);
        }
        #endregion

        #region 4. Soft Delete APIs (Hủy Phiếu Chuyển kho)
        /// <summary>
        /// Xóa mềm phiếu Chuyển kho Pending -> Chuyển sang Canceled.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpDelete("{id}/soft-delete")]
        [HttpDelete("/api/document/transfer/{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteTransferPending(long id, [FromQuery] string deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var success = await _documentService.SoftDeleteTransferPendingAsync(id, userId, deleteNote);
            return Ok(new { success, message = "Đã chuyển phiếu Chuyển kho sang trạng thái Canceled." });
        }
        #endregion
    }

    #region Supporting Request Models
    /// <summary>
    /// Model dữ liệu cho yêu cầu xử lý nhận hàng / từ chối hàng chuyển kho tại Chi nhánh Nhận.
    /// </summary>
    public class TransferReceiptRequest
    {
        public DocumentTransferStatus Status { get; set; }
        public List<DocumentDetail>? ReceivedDetails { get; set; }
        public string? Note { get; set; }
    }
    #endregion
}
