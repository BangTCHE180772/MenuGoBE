using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Controllers.Document
{
    /// <summary>
    /// Controller chuyên biệt quản lý nghiệp vụ Chứng từ Xuất bán hàng (Sale Document).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SaleDocumentController : BaseApiController
    {
        #region 0. Constructor & Dependency Injection
        private readonly IDocumentService _documentService;

        public SaleDocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }
        #endregion

        #region 1. Query APIs (Truy vấn Phiếu Xuất bán hàng)
        /// <summary>
        /// Lấy danh sách phiếu Xuất bán hàng theo Chi nhánh.
        /// </summary>
        [HttpGet]
        [HttpGet("/api/document/sale")]
        public async Task<IActionResult> GetSaleList([FromQuery] long branchId)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetSaleListAsync(branchId);
            return Ok(result);
        }
        #endregion

        #region 2. Create APIs (Tạo mới Phiếu Xuất bán hàng)
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Xuất bán hàng sang Completed (Mặc định Completed, trừ kho & ghi sổ kho).
        /// KHÔNG hỗ trợ trạng thái Pending.
        /// </summary>
        [HttpPost("completed")]
        [HttpPost("/api/document/sale/completed")]
        public async Task<IActionResult> CreateSaleCompleted([FromBody] Models.Document document)
        {
            if (document == null)
            {
                return BadRequest(new { message = "Thông tin chứng từ không được để trống." });
            }

            if (!ValidateUserAndBranchToken(document.BranchId, out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateSaleCompletedAsync(document, userId);
            return Ok(result);
        }
        #endregion
    }
}
