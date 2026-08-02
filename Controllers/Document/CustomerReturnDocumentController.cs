using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Controllers.Document
{
    /// <summary>
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Khách trả hàng (Customer Return Document).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerReturnDocumentController : BaseApiController
    {
        #region 0. Constructor & Dependency Injection
        private readonly IDocumentService _documentService;

        public CustomerReturnDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }
        #endregion

        #region 1. Query APIs (Truy vấn Phiếu Khách trả hàng)
        /// <summary>
        /// Lấy danh sách phiếu Khách trả hàng theo Chi nhánh.
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/customer-return")]
        public async Task<IActionResult> GetCustomerReturnList([FromQuery] long branchId)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetCustomerReturnListAsync(branchId);
            return Ok(result);
        }
        #endregion

        #region 2. Create APIs (Tạo mới Phiếu Khách trả hàng)
        /// <summary>
        /// Tạo & Chốt trực tiếp phiếu Khách trả hàng.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/customer-return/completed")]
        public async Task<IActionResult> CreateCustomerReturnCompleted([FromBody] Models.Document document)
        {
            if (document == null)
            {
                return BadRequest(new { message = "Thông tin chứng từ không được để trống." });
            }

            if (!ValidateUserAndBranchToken(document.BranchId, out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateCustomerReturnCompletedAsync(document, userId);
            return Ok(result);
        }
        #endregion
    }
}
