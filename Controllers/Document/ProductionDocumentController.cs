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
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Sản xuất (Production Document).
    /// Chuẩn hóa sử dụng DTO yêu cầu (ProductionDocumentCreateDto, ProductionDocumentUpdateDto) và DTO phản hồi (DocumentResponseDto).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductionDocumentController : BaseApiController
    {
        private readonly IDocumentService _documentService;

        public ProductionDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        #region GET Lấy danh sách phiếu Sản xuất DTO
        /// <summary>
        /// Lấy danh sách phiếu Sản xuất thuộc chi nhánh (Trả về List DTO gọn nhẹ).
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/production")]
        public async Task<IActionResult> GetProductionList([FromQuery] long branchId)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentListDtosAsync(branchId, DocumentType.Production);
            return Ok(result);
        }
        #endregion

        #region GET Lấy chi tiết phiếu Sản xuất DTO
        /// <summary>
        /// Lấy chi tiết 1 phiếu Sản xuất theo ID (Trả về DocumentResponseDto).
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetProductionById(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentByIdDtoAsync(id);
            if (result == null || result.Type != DocumentType.Production)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Sản xuất với ID: {id}" });
            }

            return Ok(result);
        }
        #endregion

        #region POST Tạo phiếu nháp theo DTO
        /// <summary>
        /// Tạo mới phiếu Sản xuất ở trạng thái Pending (Lưu tạm) sử dụng ProductionDocumentCreateDto.
        /// </summary>
        [HttpPost("pending")]
        [HttpPost("/api/document/production/pending")]
        public async Task<IActionResult> CreateProductionPending([FromBody] ProductionDocumentCreateDto dto)
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
                Code = dto.Code,
                BranchId = dto.BranchId,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                Type = DocumentType.Production,
                DocumentDetails = (dto.Details ?? new List<ProductionDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    FatherId = d.FatherId,
                    Quantity = d.Quantity,
                    Note = d.Note,
                    ManufactureDateSnapshot = d.ManufactureDate,
                    ExpiryDateSnapshot = d.ExpiryDate,
                    BatchCodeSnapshot = d.BatchCode
                }).ToList()
            };

            var createdDoc = await _documentService.CreateProductionPendingAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion

        #region POST Tạo & Chốt trực tiếp theo DTO
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Sản xuất sang Completed sử dụng ProductionDocumentCreateDto.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/production/completed")]
        public async Task<IActionResult> CreateProductionCompleted([FromBody] ProductionDocumentCreateDto dto)
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
                Code = dto.Code,
                BranchId = dto.BranchId,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                Type = DocumentType.Production,
                DocumentDetails = (dto.Details ?? new List<ProductionDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    FatherId = d.FatherId,
                    Quantity = d.Quantity,
                    Note = d.Note,
                    ManufactureDateSnapshot = d.ManufactureDate,
                    ExpiryDateSnapshot = d.ExpiryDate,
                    BatchCodeSnapshot = d.BatchCode
                }).ToList()
            };

            var createdDoc = await _documentService.CreateProductionCompletedAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion

        #region PUT Cập nhật phiếu nháp theo DTO
        /// <summary>
        /// Cập nhật phiếu Sản xuất ở trạng thái Pending sử dụng ProductionDocumentUpdateDto.
        /// </summary>
        [HttpPut("pending/{id:long}")]
        [HttpPut("/api/document/production/pending/{id:long}")]
        public async Task<IActionResult> UpdateProductionPending(long id, [FromBody] ProductionDocumentUpdateDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Thông tin chứng từ không được để trống." });
            }

            var existingDoc = await _documentService.GetDocumentByIdDtoAsync(id);
            if (existingDoc == null)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Sản xuất với ID: {id}" });
            }

            if (!ValidateUserAndBranchToken(existingDoc.BranchId, out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var document = new Models.Document
            {
                Id = id,
                Code = dto.Code,
                BranchId = existingDoc.BranchId,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                Type = DocumentType.Production,
                DocumentDetails = (dto.Details ?? new List<ProductionDocumentDetailUpdateDto>()).Select(d => new Models.DocumentDetail
                {
                    Id = d.Id ?? 0,
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    FatherId = d.FatherId,
                    Quantity = d.Quantity,
                    Note = d.Note,
                    ManufactureDateSnapshot = d.ManufactureDate,
                    ExpiryDateSnapshot = d.ExpiryDate,
                    BatchCodeSnapshot = d.BatchCode
                }).ToList()
            };

            var updatedDoc = await _documentService.UpdateProductionPendingAsync(id, document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(updatedDoc.Id);
            return Ok(responseDto ?? (object)updatedDoc);
        }
        #endregion

        #region POST Chốt phiếu nháp
        /// <summary>
        /// Chốt phiếu Sản xuất Pending -> Completed.
        /// </summary>
        [HttpPost("{id:long}/complete")]
        [HttpPost("/api/document/production/{id:long}/complete")]
        public async Task<IActionResult> CompleteProduction(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var completedDoc = await _documentService.CompleteProductionAsync(id, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(completedDoc.Id);
            return Ok(responseDto ?? (object)completedDoc);
        }
        #endregion

        #region DELETE Hủy phiếu nháp
        /// <summary>
        /// Xóa mềm phiếu Sản xuất Pending -> Chuyển sang Canceled.
        /// </summary>
        [HttpDelete("{id:long}/soft-delete")]
        [HttpDelete("/api/document/production/{id:long}/soft-delete")]
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
