using System;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Services;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers.Document
{
    [ApiController]
    [Route("api/document")]
    public class TransferDocumentController : ControllerBase
    {
        private readonly ITransferDocumentService _service;
        private readonly IDocumentService _documentService;

        public TransferDocumentController(ITransferDocumentService service, IDocumentService documentService)
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
        // 3. TRANSFER DOCUMENT (Chuyển kho - Type 3)
        // ==========================================
        [HttpGet("transfer")]
        public async Task<IActionResult> GetTransferDocuments([FromQuery] long? branchId)
        {
            try
            {
                var result = await _documentService.GetDocumentsAsync(branchId, DocumentType.Transfer);
                return Ok(result);
            }
            catch (MenuGoException ex)
            {
                return StatusCode(ex.HttpStatusCode, new { code = ex.Code, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi lấy danh sách phiếu xuất chuyển kho.", error = ex.Message });
            }
        }

        [HttpGet("transfer/receive")]
        [HttpGet("receive-transfer")]
        public async Task<IActionResult> GetReceiveTransferDocuments([FromQuery] long? branchId)
        {
            try
            {
                var result = await _documentService.GetDocumentsAsync(branchId, DocumentType.ReceiveTransfer);
                return Ok(result);
            }
            catch (MenuGoException ex)
            {
                return StatusCode(ex.HttpStatusCode, new { code = ex.Code, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi lấy danh sách phiếu nhận chuyển kho.", error = ex.Message });
            }
        }


        [HttpPost("transfer")]
        public async Task<IActionResult> CreateTransfer([FromBody] TransferDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = dto.AccountId ?? GetUserId();
                var result = await _service.CreateTransferDocumentAsync(dto, createdBy);
                
                return Ok(new
                {
                    message = "Tạo phiếu chuyển kho thành công (Đã tự động khởi tạo phiếu nhận ở trạng thái chờ).",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (MenuGoException ex)
            {
                return StatusCode(ex.HttpStatusCode, new { code = ex.Code, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo phiếu chuyển kho.", error = ex.Message });
            }
        }

        [HttpPost("transfer/receive")]
        [HttpPost("transfer/{id}/receive")]
        public async Task<IActionResult> ReceiveTransfer([FromBody] TransferReceiveDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = dto.AccountId ?? GetUserId();
                var result = await _service.ReceiveTransferDocumentAsync(dto, createdBy);
                
                return Ok(new
                {
                    message = "Xác nhận nhận hàng chuyển kho thành công.",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (MenuGoException ex)
            {
                return StatusCode(ex.HttpStatusCode, new { code = ex.Code, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xác nhận nhận hàng chuyển kho.", error = ex.Message });
            }
        }

        [HttpPost("transfer/reject")]
        [HttpPost("transfer/{id}/reject")]
        public async Task<IActionResult> RejectTransfer([FromBody] TransferRejectDocumentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var rejectedBy = dto.AccountId ?? GetUserId();
                var result = await _service.RejectTransferDocumentAsync(dto, rejectedBy);


                return Ok(new
                {
                    message = "Từ chối nhận hàng chuyển kho thành công. Hàng hóa đã hoàn quay trở lại kho chi nhánh gửi.",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (MenuGoException ex)
            {
                return StatusCode(ex.HttpStatusCode, new { code = ex.Code, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi từ chối nhận hàng chuyển kho.", error = ex.Message });
            }
        }

        [HttpDelete("transfer/{id}")]
        public async Task<IActionResult> DeleteTransfer(long id)
        {
            try
            {
                var deletedBy = GetUserId();
                await _service.DeleteTransferDocumentAsync(id, deletedBy);
                return Ok(new { message = "Xóa phiếu chuyển kho thành công." });
            }
            catch (MenuGoException ex)
            {
                return StatusCode(ex.HttpStatusCode, new { code = ex.Code, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa phiếu chuyển kho.", error = ex.Message });
            }
        }
    }
}

