using System;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _service;

        public DocumentController(IDocumentService service)
        {
            _service = service;
        }

        [HttpPost("import")]
        public async Task<IActionResult> CreateImport([FromBody] ImportDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdBy = User.Identity?.IsAuthenticated == true
                    ? long.Parse(User.FindFirst("AccountId")?.Value ?? "0")
                    : (long?)null;

                var result = await _service.CreateImportDocumentAsync(dto, createdBy > 0 ? createdBy : null);
                
                return Ok(new
                {
                    message = "Tạo phiếu nhập kho thành công",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Đã xảy ra lỗi khi tạo phiếu nhập kho.",
                    error = ex.Message
                });
            }
        }
    }
}
