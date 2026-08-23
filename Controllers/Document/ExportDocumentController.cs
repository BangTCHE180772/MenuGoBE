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
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Xuất dùng nội bộ (Export Document).
    /// Chuẩn hóa sử dụng DTO yêu cầu (ExportDocumentCreateDto, ExportDocumentUpdateDto) và DTO phản hồi (DocumentResponseDto).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ExportDocumentController : BaseApiController
    {
        private readonly IDocumentService _documentService;

        public ExportDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        #region GET Lấy danh sách phiếu Xuất dùng nội bộ DTO
        /// <summary>
        /// Lấy danh sách phiếu Xuất dùng nội bộ thuộc chi nhánh (Trả về List DTO gọn nhẹ).
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/export")]
        public async Task<IActionResult> GetExportList([FromQuery] long branchId)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentListDtosAsync(branchId, DocumentType.Export);
            return Ok(result);
        }
        #endregion

        #region GET Lấy chi tiết phiếu Xuất dùng nội bộ DTO
        /// <summary>
        /// Lấy chi tiết 1 phiếu Xuất dùng nội bộ theo ID (Trả về DocumentResponseDto).
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetExportById(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentByIdDtoAsync(id);
            if (result == null || result.Type != DocumentType.Export)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Xuất dùng nội bộ với ID: {id}" });
            }

            return Ok(result);
        }
        #endregion

        #region POST Tạo phiếu nháp theo DTO
        /// <summary>
        /// Tạo mới phiếu Xuất dùng nội bộ ở trạng thái Pending (Lưu tạm) sử dụng ExportDocumentCreateDto.
        /// </summary>
        [HttpPost("pending")]
        [HttpPost("/api/document/export/pending")]
        public async Task<IActionResult> CreateExportPending([FromBody] ExportDocumentCreateDto dto)
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
                Type = DocumentType.Export,
                DocumentDetails = (dto.Details ?? new List<ExportDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    Quantity = d.Quantity,
                    Note = d.Note
                }).ToList()
            };

            var createdDoc = await _documentService.CreateExportPendingAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion

        #region POST Tạo & Chốt trực tiếp theo DTO
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Xuất dùng nội bộ sang Completed sử dụng ExportDocumentCreateDto.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/export/completed")]
        public async Task<IActionResult> CreateExportCompleted([FromBody] ExportDocumentCreateDto dto)
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
                Type = DocumentType.Export,
                DocumentDetails = (dto.Details ?? new List<ExportDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    Quantity = d.Quantity,
                    Note = d.Note
                }).ToList()
            };

            var createdDoc = await _documentService.CreateExportCompletedAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion

        #region PUT Cập nhật phiếu nháp theo DTO
        /// <summary>
        /// Cập nhật phiếu Xuất dùng nội bộ ở trạng thái Pending sử dụng ExportDocumentUpdateDto.
        /// </summary>
        [HttpPut("pending/{id:long}")]
        [HttpPut("/api/document/export/pending/{id:long}")]
        public async Task<IActionResult> UpdateExportPending(long id, [FromBody] ExportDocumentUpdateDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Thông tin chứng từ không được để trống." });
            }

            var existingDoc = await _documentService.GetDocumentByIdDtoAsync(id);
            if (existingDoc == null)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Xuất dùng nội bộ với ID: {id}" });
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
                Type = DocumentType.Export,
                DocumentDetails = (dto.Details ?? new List<ExportDocumentDetailUpdateDto>()).Select(d => new Models.DocumentDetail
                {
                    Id = d.Id ?? 0,
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    Quantity = d.Quantity,
                    Note = d.Note
                }).ToList()
            };

            var updatedDoc = await _documentService.UpdateExportPendingAsync(id, document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(updatedDoc.Id);
            return Ok(responseDto ?? (object)updatedDoc);
        }
        #endregion

        #region POST Chốt phiếu nháp
        /// <summary>
        /// Chốt phiếu Xuất dùng nội bộ Pending -> Completed.
        /// </summary>
        [HttpPost("{id:long}/complete")]
        [HttpPost("/api/document/export/{id:long}/complete")]
        public async Task<IActionResult> CompleteExport(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var completedDoc = await _documentService.CompleteExportAsync(id, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(completedDoc.Id);
            return Ok(responseDto ?? (object)completedDoc);
        }
        #endregion

        #region DELETE Hủy phiếu nháp
        /// <summary>
        /// Xóa mềm phiếu Xuất dùng nội bộ Pending -> Chuyển sang Canceled.
        /// </summary>
        [HttpDelete("{id:long}/soft-delete")]
        [HttpDelete("/api/document/export/{id:long}/soft-delete")]
        public async Task<IActionResult> SoftDeleteExportPending(long id, [FromQuery] string deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var success = await _documentService.SoftDeleteExportPendingAsync(id, userId, deleteNote);
            return Ok(new { success, message = "Đã chuyển phiếu Xuất dùng nội bộ sang trạng thái Canceled." });
        }
        #endregion
    }
}
