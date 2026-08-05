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
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Xuất hủy (Export Delete / Waste Document).
    /// Chuẩn hóa sử dụng DTO yêu cầu (ExportDeleteDocumentCreateDto, ExportDeleteDocumentUpdateDto) và DTO phản hồi (DocumentResponseDto).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ExportDeleteDocumentController : BaseApiController
    {
        private readonly IDocumentService _documentService;

        public ExportDeleteDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        #region GET api/exportdeletedocument (Lấy danh sách phiếu Xuất hủy DTO)
        /// <summary>
        /// Lấy danh sách phiếu Xuất hủy thuộc chi nhánh (Trả về List DTO gọn nhẹ).
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/export-delete")]
        public async Task<IActionResult> GetExportDeleteList([FromQuery] long branchId)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentListDtosAsync(branchId, DocumentType.ExportDelete);
            return Ok(result);
        }
        #endregion

        #region GET api/exportdeletedocument/{id} (Lấy chi tiết phiếu Xuất hủy DTO)
        /// <summary>
        /// Lấy chi tiết 1 phiếu Xuất hủy theo ID (Trả về DocumentResponseDto).
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetExportDeleteById(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentByIdDtoAsync(id);
            if (result == null || result.Type != DocumentType.ExportDelete)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Xuất hủy với ID: {id}" });
            }

            return Ok(result);
        }
        #endregion

        #region POST api/exportdeletedocument/pending (Tạo phiếu nháp theo DTO)
        /// <summary>
        /// Tạo mới phiếu Xuất hủy ở trạng thái Pending (Lưu tạm) sử dụng ExportDeleteDocumentCreateDto.
        /// </summary>
        [HttpPost("pending")]
        [HttpPost("/api/document/export-delete/pending")]
        public async Task<IActionResult> CreateExportDeletePending([FromBody] ExportDeleteDocumentCreateDto dto)
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
                BranchId = dto.BranchId,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                Type = DocumentType.ExportDelete,
                DocumentDetails = (dto.Details ?? new List<ExportDeleteDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    Quantity = d.Quantity,
                    Note = d.Note
                }).ToList()
            };

            var createdDoc = await _documentService.CreateExportDeletePendingAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion

        #region POST api/exportdeletedocument/completed (Tạo & Chốt trực tiếp theo DTO)
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Xuất hủy sang Completed sử dụng ExportDeleteDocumentCreateDto.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/export-delete/completed")]
        public async Task<IActionResult> CreateExportDeleteCompleted([FromBody] ExportDeleteDocumentCreateDto dto)
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
                BranchId = dto.BranchId,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                Type = DocumentType.ExportDelete,
                DocumentDetails = (dto.Details ?? new List<ExportDeleteDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    Quantity = d.Quantity,
                    Note = d.Note
                }).ToList()
            };

            var createdDoc = await _documentService.CreateExportDeleteCompletedAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion

        #region PUT api/exportdeletedocument/pending/{id} (Cập nhật phiếu nháp theo DTO)
        /// <summary>
        /// Cập nhật phiếu Xuất hủy ở trạng thái Pending sử dụng ExportDeleteDocumentUpdateDto.
        /// </summary>
        [HttpPut("pending/{id:long}")]
        [HttpPut("/api/document/export-delete/pending/{id:long}")]
        public async Task<IActionResult> UpdateExportDeletePending(long id, [FromBody] ExportDeleteDocumentUpdateDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Thông tin chứng từ không được để trống." });
            }

            var existingDoc = await _documentService.GetDocumentByIdDtoAsync(id);
            if (existingDoc == null)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Xuất hủy với ID: {id}" });
            }

            if (!ValidateUserAndBranchToken(existingDoc.BranchId, out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var document = new Models.Document
            {
                Id = id,
                BranchId = existingDoc.BranchId,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                Type = DocumentType.ExportDelete,
                DocumentDetails = (dto.Details ?? new List<ExportDeleteDocumentDetailUpdateDto>()).Select(d => new Models.DocumentDetail
                {
                    Id = d.Id ?? 0,
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    Quantity = d.Quantity,
                    Note = d.Note
                }).ToList()
            };

            var updatedDoc = await _documentService.UpdateExportDeletePendingAsync(id, document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(updatedDoc.Id);
            return Ok(responseDto ?? (object)updatedDoc);
        }
        #endregion

        #region POST api/exportdeletedocument/{id}/complete (Chốt phiếu nháp)
        /// <summary>
        /// Chốt phiếu Xuất hủy Pending -> Completed.
        /// </summary>
        [HttpPost("{id:long}/complete")]
        [HttpPost("/api/document/export-delete/{id:long}/complete")]
        public async Task<IActionResult> CompleteExportDelete(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var completedDoc = await _documentService.CompleteExportDeleteAsync(id, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(completedDoc.Id);
            return Ok(responseDto ?? (object)completedDoc);
        }
        #endregion

        #region DELETE api/exportdeletedocument/{id}/soft-delete (Hủy phiếu nháp)
        /// <summary>
        /// Xóa mềm phiếu Xuất hủy Pending -> Chuyển sang Canceled.
        /// </summary>
        [HttpDelete("{id:long}/soft-delete")]
        [HttpDelete("/api/document/export-delete/{id:long}/soft-delete")]
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
