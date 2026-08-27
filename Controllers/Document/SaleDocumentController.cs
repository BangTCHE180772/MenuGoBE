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
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Xuất bán hàng (Sale Document).
    /// Chuẩn hóa sử dụng DTO yêu cầu (SaleDocumentCreateDto) và DTO phản hồi (DocumentResponseDto).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SaleDocumentController : BaseApiController
    {
        private readonly IDocumentService _documentService;

        public SaleDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        #region GET Lấy danh sách phiếu Xuất bán hàng DTO
        /// <summary>
        /// Lấy danh sách phiếu Xuất bán hàng thuộc chi nhánh (Trả về List DTO gọn nhẹ).
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/sale")]
        public async Task<IActionResult> GetSaleList([FromQuery] long branchId)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentListDtosAsync(branchId, DocumentType.Sale);
            return Ok(result);
        }
        #endregion

        #region GET Lấy chi tiết phiếu Xuất bán hàng DTO
        /// <summary>
        /// Lấy chi tiết 1 phiếu Xuất bán hàng theo ID (Trả về DocumentResponseDto).
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetSaleById(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentByIdDtoAsync(id);
            if (result == null || result.Type != DocumentType.Sale)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Xuất bán hàng với ID: {id}" });
            }

            return Ok(result);
        }
        #endregion

        #region POST Tạo & Chốt trực tiếp theo DTO
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Xuất bán hàng sang Completed sử dụng SaleDocumentCreateDto.
        /// KHÔNG hỗ trợ trạng thái Pending.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/sale/completed")]
        public async Task<IActionResult> CreateSaleCompleted([FromBody] SaleDocumentCreateDto dto)
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
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Note = dto.Note,
                Type = DocumentType.Sale,
                DocumentDetails = (dto.Details ?? new List<SaleDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Note = d.Note
                }).ToList()
            };

            var createdDoc = await _documentService.CreateSaleCompletedAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion
    }
}
