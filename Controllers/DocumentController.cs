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

        private long? GetUserId()
        {
            var userIdStr = User.Identity?.IsAuthenticated == true
                ? User.FindFirst("AccountId")?.Value
                : null;
            return !string.IsNullOrEmpty(userIdStr) && long.TryParse(userIdStr, out var parsedId) && parsedId > 0
                ? parsedId
                : null;
        }

        // ==========================================
        // 1. IMPORT DOCUMENT (Nhập hàng - Type 1)
        // ==========================================
        [HttpPost("import")]
        public async Task<IActionResult> CreateImport([FromBody] ImportDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = GetUserId();
                var result = await _service.CreateImportDocumentAsync(dto, createdBy);
                
                return Ok(new
                {
                    message = "Tạo phiếu nhập kho thành công.",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo phiếu nhập kho.", error = ex.Message });
            }
        }

        [HttpDelete("import/{id}")]
        public async Task<IActionResult> DeleteImport(long id)
        {
            try
            {
                var deletedBy = GetUserId();
                await _service.DeleteImportDocumentAsync(id, deletedBy);
                return Ok(new { message = "Xóa phiếu nhập kho thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa phiếu nhập kho.", error = ex.Message });
            }
        }

        // ==========================================
        // 2. EXPORT DELETE DOCUMENT (Xuất hủy - Type 2)
        // ==========================================
        [HttpPost("export-delete")]
        public async Task<IActionResult> CreateExportDelete([FromBody] ExportDeleteDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = GetUserId();
                var result = await _service.CreateExportDeleteDocumentAsync(dto, createdBy);
                
                return Ok(new
                {
                    message = "Tạo phiếu xuất hủy thành công.",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo phiếu xuất hủy.", error = ex.Message });
            }
        }

        [HttpDelete("export-delete/{id}")]
        public async Task<IActionResult> DeleteExportDelete(long id)
        {
            try
            {
                var deletedBy = GetUserId();
                await _service.DeleteExportDeleteDocumentAsync(id, deletedBy);
                return Ok(new { message = "Xóa phiếu xuất hủy thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa phiếu xuất hủy.", error = ex.Message });
            }
        }

        // ==========================================
        // 3. TRANSFER DOCUMENT (Chuyển kho - Type 3)
        // ==========================================
        [HttpPost("transfer")]
        public async Task<IActionResult> CreateTransfer([FromBody] TransferDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = GetUserId();
                var result = await _service.CreateTransferDocumentAsync(dto, createdBy);
                
                return Ok(new
                {
                    message = "Tạo phiếu chuyển kho thành công (Đang treo chờ nhận hàng).",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo phiếu chuyển kho.", error = ex.Message });
            }
        }

        [HttpPost("transfer/receive")]
        [HttpPost("transfer/{id}/receive")]
        public async Task<IActionResult> ReceiveTransfer([FromBody] TransferReceiveDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = GetUserId();
                var result = await _service.ReceiveTransferDocumentAsync(dto, createdBy);
                
                return Ok(new
                {
                    message = "Xác nhận nhận hàng chuyển kho thành công.",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xác nhận nhận hàng chuyển kho.", error = ex.Message });
            }
        }

        [HttpDelete("transfer/{id}")]
        public async Task<IActionResult> DeleteTransfer(long id)
        {
            try
            {
                var deletedBy = GetUserId();
                await _service.DeleteTransferDocumentAsync(id, deletedBy);
                return Ok(new { message = "Xóa phiếu chuyển kho thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa phiếu chuyển kho.", error = ex.Message });
            }
        }

        // ==========================================
        // 4. RETURN DOCUMENT (Trả hàng nhập - Type 4)
        // ==========================================
        [HttpPost("return")]
        public async Task<IActionResult> CreateReturn([FromBody] ReturnDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = GetUserId();
                var result = await _service.CreateReturnDocumentAsync(dto, createdBy);
                
                return Ok(new
                {
                    message = "Tạo phiếu trả hàng nhập thành công.",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo phiếu trả hàng nhập.", error = ex.Message });
            }
        }

        [HttpDelete("return/{id}")]
        public async Task<IActionResult> DeleteReturn(long id)
        {
            try
            {
                var deletedBy = GetUserId();
                await _service.DeleteReturnDocumentAsync(id, deletedBy);
                return Ok(new { message = "Xóa phiếu trả hàng nhập thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa phiếu trả hàng nhập.", error = ex.Message });
            }
        }

        // ==========================================
        // 5. CHECK DOCUMENT (Kiểm kho - Type 5)
        // ==========================================
        [HttpPost("check")]
        public async Task<IActionResult> CreateCheck([FromBody] CheckDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = GetUserId();
                var result = await _service.CreateCheckDocumentAsync(dto, createdBy);
                
                return Ok(new
                {
                    message = "Tạo phiếu kiểm kho thành công.",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo phiếu kiểm kho.", error = ex.Message });
            }
        }

        [HttpPost("adjustment")]
        public async Task<IActionResult> CreateAdjustment([FromBody] AdjustmentDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = GetUserId();
                var result = await _service.CreateAdjustmentDocumentAsync(dto, createdBy);
                
                return Ok(new
                {
                    message = "Tạo phiếu kiểm kho thành công.",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo phiếu kiểm kho.", error = ex.Message });
            }
        }

        [HttpDelete("check/{id}")]
        [HttpDelete("adjustment/{id}")]
        public async Task<IActionResult> DeleteCheck(long id)
        {
            try
            {
                var deletedBy = GetUserId();
                await _service.DeleteCheckDocumentAsync(id, deletedBy);
                return Ok(new { message = "Xóa phiếu kiểm kho thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa phiếu kiểm kho.", error = ex.Message });
            }
        }

        // ==========================================
        // 6. PRODUCTION DOCUMENT (Sản xuất - Type 6)
        // ==========================================
        [HttpPost("production")]
        public async Task<IActionResult> CreateProduction([FromBody] ProductionDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdBy = GetUserId();
                var result = await _service.CreateProductionDocumentAsync(dto, createdBy);
                
                return Ok(new
                {
                    message = "Tạo phiếu sản xuất thành công.",
                    documentId = result.Id,
                    code = result.Code
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo phiếu sản xuất.", error = ex.Message });
            }
        }

        [HttpDelete("production/{id}")]
        public async Task<IActionResult> DeleteProduction(long id)
        {
            try
            {
                var deletedBy = GetUserId();
                await _service.DeleteProductionDocumentAsync(id, deletedBy);
                return Ok(new { message = "Xóa phiếu sản xuất thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa phiếu sản xuất.", error = ex.Message });
            }
        }

        // ==========================================
        // GENERIC DELETE ENDPOINT
        // ==========================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(long id)
        {
            try
            {
                var deletedBy = GetUserId();
                await _service.DeleteDocumentAsync(id, deletedBy);
                return Ok(new { message = "Xóa chứng từ thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa chứng từ.", error = ex.Message });
            }
        }
    }
}
