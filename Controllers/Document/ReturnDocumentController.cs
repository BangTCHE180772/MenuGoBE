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
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Trả hàng NCC (Return Document).
    /// Chuẩn hóa sử dụng DTO yêu cầu (ReturnDocumentCreateDto) và DTO phản hồi (DocumentResponseDto).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ReturnDocumentController : BaseApiController
    {
        private readonly IDocumentService _documentService;

        public ReturnDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        #region GET Lấy danh sách phiếu trả hàng NCC DTO)
        /// <summary>
        /// Lấy danh sách phiếu Trả hàng NCC thuộc chi nhánh (Trả về List DTO gọn nhẹ).
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/return")]
        public async Task<IActionResult> GetReturnList([FromQuery] long branchId)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentListDtosAsync(branchId, DocumentType.Return);
            return Ok(result);
        }
        #endregion

        #region GET Lấy chi tiết phiếu trả hàng NCC DTO
        /// <summary>
        /// Lấy chi tiết 1 phiếu Trả hàng NCC theo ID (Trả về DocumentResponseDto).
        /// </summary>
        [HttpGet("{id:long}")]
        [HttpGet("/api/document/return/{id:long}")]
        public async Task<IActionResult> GetReturnById(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentByIdDtoAsync(id);
            if (result == null || result.Type != DocumentType.Return)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Trả hàng NCC với ID: {id}" });
            }

            return Ok(result);
        }
        #endregion

        #region POST Tạo & Chốt trực tiếp theo DTO
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Trả hàng NCC sang Completed sử dụng ReturnDocumentCreateDto.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/return/completed")]
        public async Task<IActionResult> CreateReturnCompleted([FromBody] ReturnDocumentCreateDto dto)
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
                PartnerId = dto.PartnerId,
                ParentDocumentId = dto.ParentDocumentId,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                AmountPaid = dto.AmountPaid,
                Type = DocumentType.Return,
                DocumentDetails = (dto.Details ?? new List<ReturnDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    FatherId = d.FatherId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Note = d.Note
                }).ToList()
            };

            var createdDoc = await _documentService.CreateReturnCompletedAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion
    }
}
