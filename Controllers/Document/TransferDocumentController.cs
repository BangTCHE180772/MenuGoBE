using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Controllers.Document
{
    /// <summary>
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Chuyển kho (Transfer Document).
    /// Chuẩn hóa sử dụng DTO yêu cầu (TransferDocumentCreateDto, TransferDocumentUpdateDto, TransferReceiptRequestDto) và DTO phản hồi (DocumentResponseDto).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TransferDocumentController : BaseApiController
    {
        private readonly IDocumentService _documentService;

        public TransferDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        #region GET Lấy danh sách phiếu Chuyển kho DTO
        /// <summary>
        /// Lấy danh sách phiếu Chuyển kho thuộc chi nhánh gửi (Trả về List DTO gọn nhẹ).
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/transfer")]
        public async Task<IActionResult> GetTransferList([FromQuery] long branchId)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentListDtosAsync(branchId, DocumentType.Transfer);
            return Ok(result);
        }
        #endregion

        #region GET Lấy chi tiết phiếu Chuyển kho DTO
        /// <summary>
        /// Lấy chi tiết 1 phiếu Chuyển kho theo ID (Trả về DocumentResponseDto).
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetTransferById(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentByIdDtoAsync(id);
            if (result == null || result.Type != DocumentType.Transfer)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Chuyển kho với ID: {id}" });
            }

            return Ok(result);
        }
        #endregion

        #region POST Tạo phiếu nháp theo DTO
        /// <summary>
        /// Tạo mới phiếu Chuyển kho ở trạng thái Pending (Lưu tạm) sử dụng TransferDocumentCreateDto.
        /// </summary>
        [HttpPost("pending")]
        [HttpPost("/api/document/transfer/pending")]
        public async Task<IActionResult> CreateTransferPending([FromBody] TransferDocumentCreateDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Thông tin chứng từ không được để trống." });
            }

            if (!ValidateUserAndBranchToken(dto.BranchId, out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var document = new Models.Document
            {
                Code = dto.Code ?? "",
                BranchId = dto.BranchId,
                ToBranchId = dto.ToBranchId,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note ?? string.Empty,
                Type = DocumentType.Transfer,
                DocumentDetails = (dto.Details ?? new List<TransferDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Note = d.Note
                }).ToList()
            };

            var createdDoc = await _documentService.CreateTransferPendingAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion

        #region POST Tạo & Chốt trực tiếp theo DTO
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Chuyển kho sang Completed tại chi nhánh gửi sử dụng TransferDocumentCreateDto.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/transfer/completed")]
        public async Task<IActionResult> CreateTransferCompleted([FromBody] TransferDocumentCreateDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Thông tin chứng từ không được để trống." });
            }

            if (!ValidateUserAndBranchToken(dto.BranchId, out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var document = new Models.Document
            {
                Code = dto.Code ?? "",
                BranchId = dto.BranchId,
                ToBranchId = dto.ToBranchId,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note ?? string.Empty,
                Type = DocumentType.Transfer,
                DocumentDetails = (dto.Details ?? new List<TransferDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Note = d.Note
                }).ToList()
            };

            var createdDoc = await _documentService.CreateTransferCompletedAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion

        #region PUT Cập nhật phiếu nháp theo DTO
        /// <summary>
        /// Cập nhật phiếu Chuyển kho ở trạng thái Pending sử dụng TransferDocumentUpdateDto.
        /// </summary>
        [HttpPut("pending/{id:long}")]
        [HttpPut("/api/document/transfer/pending/{id:long}")]
        public async Task<IActionResult> UpdateTransferPending(long id, [FromBody] TransferDocumentUpdateDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Thông tin chứng từ không được để trống." });
            }

            var existingDoc = await _documentService.GetDocumentByIdDtoAsync(id);
            if (existingDoc == null)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Chuyển kho với ID: {id}" });
            }

            if (!ValidateUserAndBranchToken(existingDoc.BranchId, out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var document = new Models.Document
            {
                Id = id,
                Code = dto.Code ?? "",
                BranchId = existingDoc.BranchId,
                ToBranchId = dto.ToBranchId,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note ?? string.Empty,
                Type = DocumentType.Transfer,
                DocumentDetails = (dto.Details ?? new List<TransferDocumentDetailUpdateDto>()).Select(d => new Models.DocumentDetail
                {
                    Id = d.Id ?? 0,
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Note = d.Note
                }).ToList()
            };

            var updatedDoc = await _documentService.UpdateTransferPendingAsync(id, document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(updatedDoc.Id);
            return Ok(responseDto ?? (object)updatedDoc);
        }
        #endregion

        #region POST Chốt phiếu nháp tại chi nhánh gửi
        /// <summary>
        /// Chốt phiếu Chuyển kho Pending -> Completed tại Chi nhánh Gửi.
        /// </summary>
        [HttpPost("{id:long}/complete")]
        [HttpPost("/api/document/transfer/{id:long}/complete")]
        public async Task<IActionResult> CompleteTransfer(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var completedDoc = await _documentService.CompleteTransferAsync(id, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(completedDoc.Id);
            return Ok(responseDto ?? (object)completedDoc);
        }
        #endregion

        #region POST Chi nhánh nhận xử lý nhận/từ chối hàng
        /// <summary>
        /// Xử lý nhận hàng / từ chối nhận hàng tại Chi nhánh Nhận khi phiếu đang ở trạng thái InTransit.
        /// </summary>
        [HttpPost("{id:long}/receive")]
        [HttpPost("/api/document/transfer/{id:long}/receive")]
        public async Task<IActionResult> ProcessTransferReceipt(long id, [FromBody] TransferReceiptRequestDto request)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            if (request == null)
            {
                return BadRequest(new { message = "Thông tin xác nhận nhận hàng không được để trống." });
            }

            if (string.IsNullOrWhiteSpace(request.Note))
            {
                return BadRequest(new { message = "Bắt buộc phải nhập Ghi chú (Note) khi xác nhận nhận hàng hoặc từ chối nhận hàng." });
            }

            var receivedDetailsList = request.ReceivedDetails?.Select(rd => new DocumentDetail
            {
                Id = rd.DetailId,
                ReceivedQuantity = rd.ReceivedQuantity
            }).ToList();

            var result = await _documentService.ProcessTransferReceiptAsync(id, request.Status, receivedDetailsList, request.Note, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(result.Id);
            return Ok(responseDto ?? (object)result);
        }
        #endregion

        #region DELETE Hủy phiếu nháp
        /// <summary>
        /// Xóa mềm phiếu Chuyển kho Pending -> Chuyển sang Canceled.
        /// </summary>
        [HttpDelete("{id:long}/soft-delete")]
        [HttpDelete("/api/document/transfer/{id:long}/soft-delete")]
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
}
