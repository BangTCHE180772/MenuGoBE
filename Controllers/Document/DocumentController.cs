using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Controllers.Document
{
    /// <summary>
    /// Controller điều hướng API dùng chung cho tất cả các loại chứng từ trong Module Document.
    /// Kế thừa BaseApiController để tự động kiểm tra và trích xuất Creator User ID từ Access Token.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : BaseApiController
    {
        #region 0. Service Dependency & Constructor
        private readonly IDocumentService _documentService;

        public DocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }
        #endregion

        #region 1. Generic Read APIs (API GET Dùng chung cho Tất cả Loại Chứng từ)
        /// <summary>
        /// Lấy danh sách chứng từ dùng chung cho màn hình dạng bảng (Grid View).
        /// Hỗ trợ lọc theo chi nhánh, loại chứng từ (type) và trạng thái (status).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDocumentList(
            [FromQuery] long branchId,
            [FromQuery] DocumentType? type = null,
            [FromQuery] DocumentStatus? status = null)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentListDtosAsync(branchId, type, status);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết thông tin 1 chứng từ bất kỳ (Import, Export, Return, Transfer,...) dưới dạng DocumentResponseDto.
        /// Phản hồi bao gồm đầy đủ Header, Partner, CashFlows và Details.
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetDocumentById(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetDocumentByIdDtoAsync(id);
            if (result == null)
            {
                return NotFound(new { message = $"Không tìm thấy chứng từ với ID: {id}" });
            }

            return Ok(result);
        }
        #endregion
    }
}
