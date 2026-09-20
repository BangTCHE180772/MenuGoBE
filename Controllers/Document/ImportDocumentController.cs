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
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Nhập kho (Import Document).
    /// Chuẩn hóa sử dụng DTO yêu cầu (ImportDocumentCreateDto, ImportDocumentUpdateDto) và DTO phản hồi (DocumentResponseDto).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ImportDocumentController : BaseApiController
    {
        private readonly IDocumentService _documentService;

        public ImportDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        #region GET Lấy danh sách phiếu nhập kho DTO
        /// <summary>
        /// Lấy danh sách phiếu Nhập kho thuộc chi nhánh (Trả về List DTO gọn nhẹ).
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/import")]
        public async Task<IActionResult> GetImportList([FromQuery] long branchId)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentListDtosAsync(branchId, DocumentType.Import);
            return Ok(result);
        }
        #endregion

        #region GET api/importdocument/{id} (Lấy chi tiết phiếu nhập kho DTO)
        /// <summary>
        /// Lấy chi tiết thông tin 1 phiếu Nhập kho theo ID (Trả về DocumentResponseDto).
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetImportById(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentByIdDtoAsync(id);
            if (result == null || result.Type != DocumentType.Import)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Nhập kho với ID: {id}" });
            }

            return Ok(result);
        }
        #endregion

        #region POST Tạo phiếu nhập nháp theo DTO
        /// <summary>
        /// Tạo mới phiếu Nhập kho ở trạng thái Pending (Lưu tạm) sử dụng ImportDocumentCreateDto.
        /// </summary>
        [HttpPost("pending")]
        [HttpPost("/api/document/import/pending")]
        public async Task<IActionResult> CreateImportPending([FromBody] ImportDocumentCreateDto dto)
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
                PartnerId = dto.PartnerId,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                AmountPaid = dto.AmountPaid,
                ImageUrls = dto.ImageUrls != null && dto.ImageUrls.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(dto.ImageUrls) : null,
                Type = DocumentType.Import,
                DocumentDetails = (dto.Details ?? new List<ImportDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    BatchCodeSnapshot = d.BatchCodeSnapshot,
                    ManufactureDateSnapshot = d.ManufactureDateSnapshot,
                    ExpiryDateSnapshot = d.ExpiryDateSnapshot,
                    Note = d.Note
                }).ToList()
            };

            var createdDoc = await _documentService.CreateImportPendingAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion

        #region POST Tạo & Chốt trực tiếp theo DTO
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Nhập kho sang Completed (Lưu thẳng / Post) sử dụng ImportDocumentCreateDto.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/import/completed")]
        public async Task<IActionResult> CreateImportCompleted([FromBody] ImportDocumentCreateDto dto)
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
                PartnerId = dto.PartnerId,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                AmountPaid = dto.AmountPaid,
                ImageUrls = dto.ImageUrls != null && dto.ImageUrls.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(dto.ImageUrls) : null,
                Type = DocumentType.Import,
                DocumentDetails = (dto.Details ?? new List<ImportDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    BatchCodeSnapshot = d.BatchCodeSnapshot,
                    ManufactureDateSnapshot = d.ManufactureDateSnapshot,
                    ExpiryDateSnapshot = d.ExpiryDateSnapshot,
                    Note = d.Note
                }).ToList()
            };

            var createdDoc = await _documentService.CreateImportCompletedAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion

        #region PUT Cập nhật phiếu nháp theo DTO
        /// <summary>
        /// Cập nhật phiếu Nhập kho đang ở trạng thái Pending sử dụng ImportDocumentUpdateDto.
        /// </summary>
        [HttpPut("pending/{id:long}")]
        [HttpPut("/api/document/import/pending/{id:long}")]
        public async Task<IActionResult> UpdateImportPending(long id, [FromBody] ImportDocumentUpdateDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Thông tin chứng từ không được để trống." });
            }

            var existingDoc = await _documentService.GetDocumentByIdDtoAsync(id);
            if (existingDoc == null)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Nhập kho với ID: {id}" });
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
                PartnerId = dto.PartnerId,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                AmountPaid = dto.AmountPaid,
                ImageUrls = dto.ImageUrls != null && dto.ImageUrls.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(dto.ImageUrls) : null,
                Type = DocumentType.Import,
                DocumentDetails = (dto.Details ?? new List<ImportDocumentDetailUpdateDto>()).Select(d => new Models.DocumentDetail
                {
                    Id = d.Id ?? 0,
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    BatchCodeSnapshot = d.BatchCodeSnapshot,
                    ManufactureDateSnapshot = d.ManufactureDateSnapshot,
                    ExpiryDateSnapshot = d.ExpiryDateSnapshot,
                    Note = d.Note
                }).ToList()
            };

            var updatedDoc = await _documentService.UpdateImportPendingAsync(id, document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(updatedDoc.Id);
            return Ok(responseDto ?? (object)updatedDoc);
        }
        #endregion

        #region POST Chốt phiếu nháp
        /// <summary>
        /// Chốt phiếu Nhập kho Pending -> Completed (Đóng băng Snapshot, Ghi sổ kho & CashFlow).
        /// </summary>
        [HttpPost("{id:long}/complete")]
        [HttpPost("/api/document/import/{id:long}/complete")]
        public async Task<IActionResult> CompleteImport(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var completedDoc = await _documentService.CompleteImportAsync(id, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(completedDoc.Id);
            return Ok(responseDto ?? (object)completedDoc);
        }
        #endregion

        #region DELETE Hủy phiếu nháp
        /// <summary>
        /// Xóa mềm phiếu Nhập kho Pending -> Chuyển sang Canceled.
        /// </summary>
        [HttpDelete("{id:long}/soft-delete")]
        [HttpDelete("/api/document/import/{id:long}/soft-delete")]
        public async Task<IActionResult> SoftDeleteImportPending(long id, [FromQuery] string deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.SoftDeleteImportPendingAsync(id, userId, deleteNote);
            return Ok(new { success = result, message = "Đã hủy phiếu nhập nháp thành công." });
        }
        #endregion

        #region GET Lấy thông tin trả hàng cho phiếu Nhập
        /// <summary>
        /// Lấy danh sách sản phẩm trong phiếu Nhập kho đã chốt kèm số lượng đã trả tích lũy,
        /// phục vụ tạo phiếu Trả hàng NCC.
        /// Toàn bộ dữ liệu sản phẩm lấy từ Snapshot (không phụ thuộc master data hiện tại).
        /// </summary>
        [HttpGet("{id:long}/return-details")]
        [HttpGet("/api/document/import/{id:long}/return-details")]
        public async Task<IActionResult> GetImportReturnDetails(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetImportReturnDetailsAsync(id);
            if (result == null)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Nhập kho với ID: {id}" });
            }

            return Ok(result);
        }
        #endregion
    }
}
