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
    /// Controller chuyển đổi quản lý nhập về Chứng từ Kiểm kho (Inventory Check Document).
    /// Chuẩn hóa sử dụng DTO yêu cầu (CheckDocumentCreateDto, CheckDocumentUpdateDto) và DTO phản hồi (DocumentResponseDto).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CheckDocumentController : BaseApiController
    {
        private readonly IDocumentService _documentService;

        public CheckDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        #region GET Lấy danh sách phiếu Kiểm kho DTO
        /// <summary>
        /// Lấy danh sách phiếu Kiểm kho thuộc chi nhánh (Trả về List DTO gọn nhẹ).
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/check")]
        public async Task<IActionResult> GetCheckList([FromQuery] long branchId)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentListDtosAsync(branchId, DocumentType.Check);
            return Ok(result);
        }
        #endregion

        #region GET Lấy chi tiết phiếu Kiểm kho DTO
        /// <summary>
        /// Lấy chi tiết 1 phiếu Kiểm kho theo ID (Trả về DocumentResponseDto).
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetCheckById(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentByIdDtoAsync(id);
            if (result == null || result.Type != DocumentType.Check)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Kiểm kho với ID: {id}" });
            }

            return Ok(result);
        }
        #endregion

        #region POST Tạo phiếu nháp theo DTO
        /// <summary>
        /// Tạo mới phiếu Kiểm kho ở trạng thái Pending (Lưu tạm) sử dụng CheckDocumentCreateDto.
        /// </summary>
        [HttpPost("pending")]
        [HttpPost("/api/document/check/pending")]
        public async Task<IActionResult> CreateCheckPending([FromBody] CheckDocumentCreateDto dto)
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
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                Type = DocumentType.Check,
                DocumentDetails = (dto.Details ?? new List<CheckDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    BatchCodeSnapshot = d.BatchCodeSnapshot,
                    ManufactureDateSnapshot = d.ManufactureDateSnapshot,
                    ExpiryDateSnapshot = d.ExpiryDateSnapshot,
                    Quantity = d.ActualQuantity,
                    ActualQuantity = d.ActualQuantity,
                    Note = d.Note
                }).ToList()
            };

            var createdDoc = await _documentService.CreateCheckPendingAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion

        #region POST Tạo & Chốt trực tiếp theo DTO
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Kiểm kho sang Completed sử dụng CheckDocumentCreateDto.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/check/completed")]
        public async Task<IActionResult> CreateCheckCompleted([FromBody] CheckDocumentCreateDto dto)
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
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                Type = DocumentType.Check,
                DocumentDetails = (dto.Details ?? new List<CheckDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    BatchCodeSnapshot = d.BatchCodeSnapshot,
                    ManufactureDateSnapshot = d.ManufactureDateSnapshot,
                    ExpiryDateSnapshot = d.ExpiryDateSnapshot,
                    Quantity = d.ActualQuantity,
                    ActualQuantity = d.ActualQuantity,
                    Note = d.Note
                }).ToList()
            };

            var createdDoc = await _documentService.CreateCheckCompletedAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion

        #region PUT Cập nhật phiếu nháp theo DTO
        /// <summary>
        /// Cập nhật phiếu Kiểm kho ở trạng thái Pending sử dụng CheckDocumentUpdateDto.
        /// </summary>
        [HttpPut("pending/{id:long}")]
        [HttpPut("/api/document/check/pending/{id:long}")]
        public async Task<IActionResult> UpdateCheckPending(long id, [FromBody] CheckDocumentUpdateDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Thông tin chứng từ không được để trống." });
            }

            var existingDoc = await _documentService.GetDocumentByIdDtoAsync(id);
            if (existingDoc == null)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Kiểm kho với ID: {id}" });
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
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                Type = DocumentType.Check,
                DocumentDetails = (dto.Details ?? new List<CheckDocumentDetailUpdateDto>()).Select(d => new Models.DocumentDetail
                {
                    Id = d.Id ?? 0,
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    BatchCodeSnapshot = d.BatchCodeSnapshot,
                    ManufactureDateSnapshot = d.ManufactureDateSnapshot,
                    ExpiryDateSnapshot = d.ExpiryDateSnapshot,
                    Quantity = d.ActualQuantity,
                    ActualQuantity = d.ActualQuantity,
                    Note = d.Note
                }).ToList()
            };

            var updatedDoc = await _documentService.UpdateCheckPendingAsync(id, document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(updatedDoc.Id);
            return Ok(responseDto ?? (object)updatedDoc);
        }
        #endregion

        #region POST Chốt phiếu nháp
        /// <summary>
        /// Chốt phiếu Kiểm kho Pending -> Completed.
        /// </summary>
        [HttpPost("{id:long}/complete")]
        [HttpPost("/api/document/check/{id:long}/complete")]
        public async Task<IActionResult> CompleteCheck(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var completedDoc = await _documentService.CompleteCheckAsync(id, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(completedDoc.Id);
            return Ok(responseDto ?? (object)completedDoc);
        }
        #endregion

        #region DELETE Hủy phiếu nháp
        /// <summary>
        /// Xóa mềm phiếu Kiểm kho Pending -> Chuyển sang Canceled.
        /// </summary>
        [HttpDelete("{id:long}/soft-delete")]
        [HttpDelete("/api/document/check/{id:long}/soft-delete")]
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
