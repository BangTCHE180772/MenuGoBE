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
        // Nghiệp vụ:
        //   - Mỗi phiếu trả thuộc về 1 phiếu nhập gốc (ParentDocumentId)
        //   - Mỗi ReturnDetail gắn với ImportDetail cụ thể qua FatherId
        //   - Hạn mức trả: ImportDetail.Quantity - SUM(active ReturnDetails.Quantity)
        //   - CashFlow: chỉ tạo khi PaidAmount > 0
        // ==========================================

        /// <summary>
        /// Lấy danh sách tất cả phiếu trả hàng nhập của một chi nhánh.
        /// </summary>
        [HttpGet("return")]
        public async Task<IActionResult> GetReturnDocuments([FromQuery] long? branchId)
        {
            try
            {
                var result = await _documentService.GetDocumentsAsync(branchId, DocumentType.Return);
                return Ok(result);
            }
            catch (MenuGoException ex)
            {
                return StatusCode(ex.HttpStatusCode, new { code = ex.Code, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi lấy danh sách phiếu trả hàng nhập.", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chi tiết một phiếu trả hàng nhập theo Id.
        /// </summary>
        [HttpGet("return/{id:long}")]
        public async Task<IActionResult> GetReturnDocument(long id)
        {
            try
            {
                var result = await _documentService.GetDocumentByIdAsync(id);
                if (result == null)
                    return NotFound(new { message = $"Không tìm thấy phiếu trả hàng nhập với Id={id}." });

                return Ok(result);
            }
            catch (MenuGoException ex)
            {
                return StatusCode(ex.HttpStatusCode, new { code = ex.Code, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi lấy chi tiết phiếu trả hàng nhập.", error = ex.Message });
            }
        }

        /// <summary>
        /// Tạo phiếu trả hàng nhập mới.
        /// Body cần: ImportDocumentId, BusinessDate, PaidAmount (nếu có), PaymentMethod, Details[].
        /// Mỗi Detail: ImportDocumentDetailId, Quantity, UnitPrice.
        /// </summary>
        [HttpPost("return")]
        public async Task<IActionResult> CreateReturn([FromBody] ReturnDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = dto.AccountId ?? GetUserId();
                var result = await _service.CreateReturnDocumentAsync(dto, createdBy);

                return Ok(new
                {
                    message = "Tạo phiếu trả hàng nhập thành công.",
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
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo phiếu trả hàng nhập.", error = ex.Message });
            }
        }

        /// <summary>
        /// Xóa mềm phiếu trả hàng nhập.
        /// Sau khi xóa: IsDeleted = true cho Document + tất cả ReturnDetails,
        /// InventoryLedger được tính lại (ripple recalculate).
        /// </summary>
        [HttpDelete("return/{id:long}")]
        public async Task<IActionResult> DeleteReturn(long id)
        {
            try
            {
                var deletedBy = GetUserId();
                await _service.DeleteReturnDocumentAsync(id, deletedBy);
                return Ok(new { message = "Xóa phiếu trả hàng nhập thành công." });
            }
            catch (MenuGoException ex)
            {
                return StatusCode(ex.HttpStatusCode, new { code = ex.Code, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa phiếu trả hàng nhập.", error = ex.Message });
            }
        }
    }
}
