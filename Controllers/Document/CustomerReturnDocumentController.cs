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
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Khách trả hàng (Customer Return Document).
    /// Chuẩn hóa sử dụng DTO yêu cầu (CustomerReturnDocumentCreateDto) và DTO phản hồi (DocumentResponseDto).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerReturnDocumentController : BaseApiController
    {
        private readonly IDocumentService _documentService;

        public CustomerReturnDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        #region GET Lấy danh sách phiếu Khách trả hàng DTO
        /// <summary>
        /// Lấy danh sách phiếu Khách trả hàng thuộc chi nhánh (Trả về List DTO gọn nhẹ).
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/customer-return")]
        public async Task<IActionResult> GetCustomerReturnList([FromQuery] long branchId)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentListDtosAsync(branchId, DocumentType.CustomerReturn);
            return Ok(result);
        }
        #endregion

        #region GET Lấy chi tiết phiếu Khách trả hàng DTO
        /// <summary>
        /// Lấy chi tiết 1 phiếu Khách trả hàng theo ID (Trả về DocumentResponseDto).
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetCustomerReturnById(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentByIdDtoAsync(id);
            if (result == null || result.Type != DocumentType.CustomerReturn)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu Khách trả hàng với ID: {id}" });
            }

            return Ok(result);
        }
        #endregion

        #region POST Tạo & Chốt trực tiếp theo DTO
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Khách trả hàng sang Completed sử dụng CustomerReturnDocumentCreateDto.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/customer-return/completed")]
        public async Task<IActionResult> CreateCustomerReturnCompleted([FromBody] CustomerReturnDocumentCreateDto dto)
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
                Type = DocumentType.CustomerReturn,
                DocumentDetails = (dto.Details ?? new List<CustomerReturnDocumentDetailCreateDto>()).Select(d => new Models.DocumentDetail
                {
                    BInventoryId = d.BInventoryId,
                    UnitConversionId = d.UnitConversionId,
                    FatherId = d.FatherId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Note = d.Note
                }).ToList()
            };

            var createdDoc = await _documentService.CreateCustomerReturnCompletedAsync(document, userId);
            var responseDto = await _documentService.GetDocumentByIdDtoAsync(createdDoc.Id);
            return Ok(responseDto ?? (object)createdDoc);
        }
        #endregion
    }
}
