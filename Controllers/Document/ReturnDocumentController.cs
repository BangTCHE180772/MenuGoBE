using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Controllers.Document
{
    /// <summary>
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Trả hàng NCC (Return Document).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ReturnDocumentController : BaseApiController
    {
        #region 0. Constructor & Dependency Injection
        private readonly IDocumentService _documentService;

        public ReturnDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }
        #endregion

        #region 1. Query APIs (Truy vấn Phiếu Trả hàng NCC)
        /// <summary>
        /// Lấy danh sách phiếu Trả hàng NCC theo Chi nhánh.
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/return")]
        public async Task<IActionResult> GetReturnList([FromQuery] long branchId)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetReturnListAsync(branchId);
            return Ok(result);
        }
        #endregion

        #region 2. Create APIs (Tạo mới Phiếu Trả hàng NCC)
        /// <summary>
        /// Tạo & Chốt trực tiếp phiếu Trả hàng NCC.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/return/completed")]
        public async Task<IActionResult> CreateReturnCompleted([FromBody] Models.Document document)
        {
            if (document == null)
            {
                return BadRequest(new { message = "Thông tin chứng từ không được để trống." });
            }

            if (!ValidateUserAndBranchToken(document.BranchId, out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateReturnCompletedAsync(document, userId);
            return Ok(result);
        }
        #endregion
    }
}
