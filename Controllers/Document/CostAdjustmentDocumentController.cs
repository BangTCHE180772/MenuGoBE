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
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Điều chỉnh giá vốn (Cost Adjustment Document).
    /// Chuẩn hóa sử dụng DTO yêu cầu (CostAdjustmentDocumentCreateDto, CostAdjustmentDocumentUpdateDto) và DTO phản hồi (DocumentResponseDto).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CostAdjustmentDocumentController : BaseApiController
    {
        private readonly IDocumentService _documentService;

        public CostAdjustmentDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        #region GET Lấy danh sách phiếu Điều chỉnh giá vốn DTO
        /// <summary>
        /// Lấy danh sách phiếu Điều chỉnh giá vốn thuộc chi nhánh (Trả về List DTO gọn nhẹ).
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/cost-adjustment")]
        public async Task<IActionResult> GetCostAdjustmentList([FromQuery] long branchId)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentListDtosAsync(branchId, DocumentType.CostAdjustment);
            return Ok(result);
        }
        #endregion

        #region GET Lấy chi tiết phiếu Điều chỉnh giá vốn DTO
        /// <summary>
        /// Lấy chi tiết 1 phiếu Điều chỉnh giá vốn theo ID (Trả về DocumentResponseDto).
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetCostAdjustmentById(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentByIdDtoAsync(id);
            if (result == null || result.Type != DocumentType.CostAdjustment)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Điều chỉnh giá vốn với ID: {id}" });
            }

            return Ok(result);
        }
        #endregion

        #region POST Tạo phiếu nháp theo DTO
        /// <summary>
        /// Tạo mới phiếu Điều chỉnh giá vốn ở trạng thái Pending (Lưu tạm) sử dụng CostAdjustmentDocumentCreateDto.
        /// </summary>
        [HttpPost("pending")]
        [HttpPost("/api/document/cost-adjustment/pending")]
        public async Task<IActionResult> CreateCostAdjustmentPending([FromBody] CostAdjustmentDocumentCreateDto dto)
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
                Code = dto.Code ?? string.Empty,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                Type = DocumentType.CostAdjustment,
                DocumentDetails = (dto.Details ?? new List<CostAdjustmentDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    NewAvgCost = d.NewAvgCost,
                    Note = d.Note
                }).ToList()
            };

            var createdDoc = await _documentService.CreateCostAdjustmentPendingAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion

        #region POST Tạo & Chốt trực tiếp theo DTO
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Điều chỉnh giá vốn sang Completed sử dụng CostAdjustmentDocumentCreateDto.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/cost-adjustment/completed")]
        public async Task<IActionResult> CreateCostAdjustmentCompleted([FromBody] CostAdjustmentDocumentCreateDto dto)
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
                Code = dto.Code ?? string.Empty,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                Type = DocumentType.CostAdjustment,
                DocumentDetails = (dto.Details ?? new List<CostAdjustmentDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    NewAvgCost = d.NewAvgCost,
                    Note = d.Note
                }).ToList()
            };

            var createdDoc = await _documentService.CreateCostAdjustmentCompletedAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion

        #region PUT Cập nhật phiếu nháp theo DTO
        /// <summary>
        /// Cập nhật phiếu Điều chỉnh giá vốn ở trạng thái Pending sử dụng CostAdjustmentDocumentUpdateDto.
        /// </summary>
        [HttpPut("pending/{id:long}")]
        [HttpPut("/api/document/cost-adjustment/pending/{id:long}")]
        public async Task<IActionResult> UpdateCostAdjustmentPending(long id, [FromBody] CostAdjustmentDocumentUpdateDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Thông tin chứng từ không được để trống." });
            }

            var existingDoc = await _documentService.GetDocumentByIdDtoAsync(id);
            if (existingDoc == null)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Điều chỉnh giá vốn với ID: {id}" });
            }

            if (!ValidateUserAndBranchToken(existingDoc.BranchId, out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var document = new Models.Document
            {
                Id = id,
                BranchId = existingDoc.BranchId,
                Code = dto.Code ?? string.Empty,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                Type = DocumentType.CostAdjustment,
                DocumentDetails = (dto.Details ?? new List<CostAdjustmentDocumentDetailUpdateDto>()).Select(d => new Models.DocumentDetail
                {
                    Id = d.Id ?? 0,
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    NewAvgCost = d.NewAvgCost,
                    Note = d.Note
                }).ToList()
            };

            var updatedDoc = await _documentService.UpdateCostAdjustmentPendingAsync(id, document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(updatedDoc.Id);
            return Ok(responseDto ?? (object)updatedDoc);
        }
        #endregion

        #region POST Chốt phiếu nháp
        /// <summary>
        /// Chốt phiếu Điều chỉnh giá vốn Pending -> Completed.
        /// </summary>
        [HttpPost("{id:long}/complete")]
        [HttpPost("/api/document/cost-adjustment/{id:long}/complete")]
        public async Task<IActionResult> CompleteCostAdjustment(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var completedDoc = await _documentService.CompleteCostAdjustmentAsync(id, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(completedDoc.Id);
            return Ok(responseDto ?? (object)completedDoc);
        }
        #endregion

        #region DELETE Hủy phiếu nháp
        /// <summary>
        /// Xóa mềm phiếu Điều chỉnh giá vốn Pending -> Chuyển sang Canceled.
        /// </summary>
        [HttpDelete("{id:long}/soft-delete")]
        [HttpDelete("/api/document/cost-adjustment/{id:long}/soft-delete")]
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
