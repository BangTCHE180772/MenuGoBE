using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Controllers.Document
{
    /// <summary>
    /// Controller điều hướng API riêng biệt cho các loại chứng từ trong Module Document.
    /// Kế thừa BaseApiController để tự động kiểm tra và trích xuất Creator User ID từ Access Token.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : BaseApiController
    {
        #region Khai báo Service & Constructor

        #region Service Dependency
        /// <summary>
        /// Interface DocumentService quản lý quy trình nghiệp vụ chứng từ.
        /// </summary>
        private readonly IDocumentService _documentService;
        #endregion

        #region Constructor
        /// <summary>
        /// Khởi tạo DocumentController với IDocumentService.
        /// </summary>
        /// <param name="documentService">Service chứng từ</param>
        public DocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }
        #endregion

        #endregion

        #region 0. Generic Read APIs (API GET Dùng chung cho Tất cả Loại Chứng từ)

        #region GET api/document (Danh sách dùng chung)
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
        #endregion

        #region GET api/document/{id} (Chi tiết dùng chung)
        /// <summary>
        /// Lấy chi tiết thông tin 1 chứng từ bất kỳ (Import, Export, Return, Transfer,...) dưới dạng DocumentResponseDto.
        /// Phản hồi bao gồm đầy đủ Header, Partner, CashFlows và Details (Snapshot nếu Completed, Ref IDs + Current Live Data nếu Pending).
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

        #endregion

        #region 1. Nghiệp vụ Chứng từ Nhập kho (Import API)

        #region GET api/document/import
        /// <summary>
        /// Lấy danh sách phiếu Nhập kho theo Chi nhánh.
        /// </summary>
        [HttpGet("import")]
        public async Task<IActionResult> GetImportList([FromQuery] long branchId)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.GetImportListAsync(branchId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/import/pending
        /// <summary>
        /// Tạo mới phiếu Nhập kho ở trạng thái Pending (Lưu tạm).
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("import/pending")]
        public async Task<IActionResult> CreateImportPending([FromBody] Models.Document document)
        {
            if (document == null)
            {
                return BadRequest(new { message = "Thông tin chứng từ không được để trống." });
            }

            if (!ValidateUserAndBranchToken(document.BranchId, out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateImportPendingAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/import/completed
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Nhập kho sang Completed (Lưu thẳng / Post).
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("import/completed")]
        public async Task<IActionResult> CreateImportCompleted([FromBody] Models.Document document)
        {
            if (document == null)
            {
                return BadRequest(new { message = "Thông tin chứng từ không được để trống." });
            }

            if (!ValidateUserAndBranchToken(document.BranchId, out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateImportCompletedAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region PUT api/document/import/pending/{id}
        /// <summary>
        /// Cập nhật phiếu Nhập kho đang ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPut("import/pending/{id}")]
        public async Task<IActionResult> UpdateImportPending(long id, [FromBody] Models.Document document)
        {
            if (document == null)
            {
                return BadRequest(new { message = "Thông tin chứng từ không được để trống." });
            }

            if (!ValidateUserAndBranchToken(document.BranchId, out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.UpdateImportPendingAsync(id, document, userId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/import/{id}/complete
        /// <summary>
        /// Chốt phiếu Nhập kho Pending -> Completed (Ghi sổ kho & phát sinh CashFlow).
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("import/{id}/complete")]
        public async Task<IActionResult> CompleteImport(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CompleteImportAsync(id, userId);
            return Ok(result);
        }
        #endregion

        #region DELETE api/document/import/{id}/soft-delete
        /// <summary>
        /// Xóa mềm phiếu Nhập kho Pending -> Chuyển sang Canceled.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpDelete("import/{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteImportPending(long id, [FromQuery] string deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var success = await _documentService.SoftDeleteImportPendingAsync(id, userId, deleteNote);
            return Ok(new { success, message = "Đã chuyển phiếu Nhập kho sang trạng thái Canceled." });
        }
        #endregion

        #endregion

        #region 2. Nghiệp vụ Chứng từ Trả hàng NCC (Return API)

        #region GET api/document/return
        /// <summary>
        /// Lấy danh sách phiếu Trả hàng NCC theo Chi nhánh.
        /// </summary>
        [HttpGet("return")]
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

        #region POST api/document/return/completed
        /// <summary>
        /// Tạo & Chốt trực tiếp phiếu Trả hàng NCC.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("return/completed")]
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

        #endregion

        #region 3. Nghiệp vụ Chứng từ Xuất bán hàng (Sale API)

        #region GET api/document/sale
        /// <summary>
        /// Lấy danh sách phiếu Xuất bán hàng theo Chi nhánh.
        /// </summary>
        [HttpGet("sale")]
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

        #region POST api/document/sale/completed
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Xuất bán hàng sang Completed (Mặc định Completed, trừ kho & ghi sổ kho).
        /// KHÔNG hỗ trợ trạng thái Pending.
        /// </summary>
        [HttpPost("sale/completed")]
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

        #endregion

        #region 4. Nghiệp vụ Chứng từ Khách trả hàng (CustomerReturn API)

        #region GET api/document/customer-return
        /// <summary>
        /// Lấy danh sách phiếu Khách trả hàng theo Chi nhánh.
        /// </summary>
        [HttpGet("customer-return")]
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

        #region POST api/document/customer-return/completed
        /// <summary>
        /// Tạo & Chốt trực tiếp phiếu Khách trả hàng.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("customer-return/completed")]
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

        #endregion

        #region 5. Nghiệp vụ Chứng từ Chuyển kho (Transfer API)

        #region GET api/document/transfer
        /// <summary>
        /// Lấy danh sách phiếu Chuyển kho theo Chi nhánh gửi.
        /// </summary>
        [HttpGet("transfer")]
        public async Task<IActionResult> GetTransferList([FromQuery] long branchId)
        {
            var result = await _documentService.GetTransferListAsync(branchId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/transfer/pending
        /// <summary>
        /// Tạo mới phiếu Chuyển kho ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("transfer/pending")]
        public async Task<IActionResult> CreateTransferPending([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateTransferPendingAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/transfer/completed
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Chuyển kho sang Completed tại Chi nhánh Gửi.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("transfer/completed")]
        public async Task<IActionResult> CreateTransferCompleted([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateTransferCompletedAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region PUT api/document/transfer/pending/{id}
        /// <summary>
        /// Cập nhật phiếu Chuyển kho ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPut("transfer/pending/{id}")]
        public async Task<IActionResult> UpdateTransferPending(long id, [FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.UpdateTransferPendingAsync(id, document, userId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/transfer/{id}/complete
        /// <summary>
        /// Chốt phiếu Chuyển kho Pending -> Completed tại Chi nhánh Gửi.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("transfer/{id}/complete")]
        public async Task<IActionResult> CompleteTransfer(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CompleteTransferAsync(id, userId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/transfer/{id}/receive
        /// <summary>
        /// Xử lý nhận hàng / từ chối nhận hàng tại Chi nhánh Nhận khi phiếu đang ở trạng thái InTransit.
        /// Chấp nhận 3 trạng thái: Received, PartialReceived, Rejected.
        /// </summary>
        [HttpPost("transfer/{id}/receive")]
        public async Task<IActionResult> ProcessTransferReceipt(long id, [FromBody] TransferReceiptRequest request)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            if (request == null)
            {
                return BadRequest(new { message = "Thông tin xác nhận nhận hàng không được để trống." });
            }

            var result = await _documentService.ProcessTransferReceiptAsync(id, request.Status, request.ReceivedDetails, request.Note, userId);
            return Ok(result);
        }
        #endregion

        #region DELETE api/document/transfer/{id}/soft-delete
        /// <summary>
        /// Xóa mềm phiếu Chuyển kho Pending -> Chuyển sang Canceled.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpDelete("transfer/{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteTransferPending(long id, [FromQuery] string deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var success = await _documentService.SoftDeleteTransferPendingAsync(id, userId, deleteNote);
            return Ok(new { success, message = "Đã chuyển phiếu Chuyển kho sang trạng thái Canceled." });
        }
        #endregion

        #endregion

        #region 6. Nghiệp vụ Chứng từ Xuất hủy (Export API)

        #region GET api/document/export
        /// <summary>
        /// Lấy danh sách phiếu Xuất hủy theo Chi nhánh.
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> GetExportList([FromQuery] long branchId)
        {
            var result = await _documentService.GetExportListAsync(branchId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/export/pending
        /// <summary>
        /// Tạo mới phiếu Xuất hủy ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("export/pending")]
        public async Task<IActionResult> CreateExportPending([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateExportPendingAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region PUT api/document/export/pending/{id}
        /// <summary>
        /// Cập nhật phiếu Xuất hủy ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPut("export/pending/{id}")]
        public async Task<IActionResult> UpdateExportPending(long id, [FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.UpdateExportPendingAsync(id, document, userId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/export/{id}/complete
        /// <summary>
        /// Chốt phiếu Xuất hủy Pending -> Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("export/{id}/complete")]
        public async Task<IActionResult> CompleteExport(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CompleteExportAsync(id, userId);
            return Ok(result);
        }
        #endregion

        #region DELETE api/document/export/{id}/soft-delete
        /// <summary>
        /// Xóa mềm phiếu Xuất hủy Pending -> Chuyển sang Canceled.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpDelete("export/{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteExportPending(long id, [FromQuery] string deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var success = await _documentService.SoftDeleteExportPendingAsync(id, userId, deleteNote);
            return Ok(new { success, message = "Đã chuyển phiếu Xuất dùng nội bộ sang trạng thái Canceled." });
        }
        #endregion

        #endregion

        #region 6b. Nghiệp vụ Chứng từ Xuất hủy (ExportDelete API)

        #region GET api/document/export-delete
        /// <summary>
        /// Lấy danh sách phiếu Xuất hủy theo Chi nhánh.
        /// </summary>
        [HttpGet("export-delete")]
        public async Task<IActionResult> GetExportDeleteList([FromQuery] long branchId)
        {
            var result = await _documentService.GetExportDeleteListAsync(branchId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/export-delete/pending
        /// <summary>
        /// Tạo mới phiếu Xuất hủy ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("export-delete/pending")]
        public async Task<IActionResult> CreateExportDeletePending([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateExportDeletePendingAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/export-delete/completed
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Xuất hủy sang Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("export-delete/completed")]
        public async Task<IActionResult> CreateExportDeleteCompleted([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateExportDeleteCompletedAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region PUT api/document/export-delete/pending/{id}
        /// <summary>
        /// Cập nhật phiếu Xuất hủy ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPut("export-delete/pending/{id}")]
        public async Task<IActionResult> UpdateExportDeletePending(long id, [FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.UpdateExportDeletePendingAsync(id, document, userId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/export-delete/{id}/complete
        /// <summary>
        /// Chốt phiếu Xuất hủy Pending -> Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("export-delete/{id}/complete")]
        public async Task<IActionResult> CompleteExportDelete(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CompleteExportDeleteAsync(id, userId);
            return Ok(result);
        }
        #endregion

        #region DELETE api/document/export-delete/{id}/soft-delete
        /// <summary>
        /// Xóa mềm phiếu Xuất hủy Pending -> Chuyển sang Canceled.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpDelete("export-delete/{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteExportDeletePending(long id, [FromQuery] string deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var success = await _documentService.SoftDeleteExportDeletePendingAsync(id, userId, deleteNote);
            return Ok(new { success, message = "Đã chuyển phiếu Xuất hủy sang trạng thái Canceled." });
        }
        #endregion

        #endregion

        #region 7. Nghiệp vụ Chứng từ Sản xuất (Production API)

        #region GET api/document/production
        /// <summary>
        /// Lấy danh sách phiếu Sản xuất theo Chi nhánh.
        /// </summary>
        [HttpGet("production")]
        public async Task<IActionResult> GetProductionList([FromQuery] long branchId)
        {
            var result = await _documentService.GetProductionListAsync(branchId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/production/pending
        /// <summary>
        /// Tạo mới phiếu Sản xuất ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("production/pending")]
        public async Task<IActionResult> CreateProductionPending([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateProductionPendingAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/production/completed
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Sản xuất sang Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("production/completed")]
        public async Task<IActionResult> CreateProductionCompleted([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateProductionCompletedAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region PUT api/document/production/pending/{id}
        /// <summary>
        /// Cập nhật phiếu Sản xuất ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPut("production/pending/{id}")]
        public async Task<IActionResult> UpdateProductionPending(long id, [FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.UpdateProductionPendingAsync(id, document, userId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/production/{id}/complete
        /// <summary>
        /// Chốt phiếu Sản xuất Pending -> Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("production/{id}/complete")]
        public async Task<IActionResult> CompleteProduction(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CompleteProductionAsync(id, userId);
            return Ok(result);
        }
        #endregion

        #region DELETE api/document/production/{id}/soft-delete
        /// <summary>
        /// Xóa mềm phiếu Sản xuất Pending -> Chuyển sang Canceled.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpDelete("production/{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteProductionPending(long id, [FromQuery] string deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var success = await _documentService.SoftDeleteProductionPendingAsync(id, userId, deleteNote);
            return Ok(new { success, message = "Đã chuyển phiếu Sản xuất sang trạng thái Canceled." });
        }
        #endregion

        #endregion

        #region 8. Nghiệp vụ Chứng từ Kiểm kho (Check API)

        #region GET api/document/check
        /// <summary>
        /// Lấy danh sách phiếu Kiểm kho theo Chi nhánh.
        /// </summary>
        [HttpGet("check")]
        public async Task<IActionResult> GetCheckList([FromQuery] long branchId)
        {
            var result = await _documentService.GetCheckListAsync(branchId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/check/pending
        /// <summary>
        /// Tạo mới phiếu Kiểm kho ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("check/pending")]
        public async Task<IActionResult> CreateCheckPending([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateCheckPendingAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/check/completed
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Kiểm kho sang Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("check/completed")]
        public async Task<IActionResult> CreateCheckCompleted([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateCheckCompletedAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region PUT api/document/check/pending/{id}
        /// <summary>
        /// Cập nhật phiếu Kiểm kho ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPut("check/pending/{id}")]
        public async Task<IActionResult> UpdateCheckPending(long id, [FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.UpdateCheckPendingAsync(id, document, userId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/check/{id}/complete
        /// <summary>
        /// Chốt phiếu Kiểm kho Pending -> Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("check/{id}/complete")]
        public async Task<IActionResult> CompleteCheck(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CompleteCheckAsync(id, userId);
            return Ok(result);
        }
        #endregion

        #region DELETE api/document/check/{id}/soft-delete
        /// <summary>
        /// Xóa mềm phiếu Kiểm kho Pending -> Chuyển sang Canceled.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpDelete("check/{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteCheckPending(long id, [FromQuery] string deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var success = await _documentService.SoftDeleteCheckPendingAsync(id, userId, deleteNote);
            return Ok(new { success, message = "Đã chuyển phiếu Kiểm kho sang trạng thái Canceled." });
        }
        #endregion

        #endregion

        #region 9. Nghiệp vụ Chứng từ Điều chỉnh giá vốn (CostAdjustment API)

        #region GET api/document/cost-adjustment
        /// <summary>
        /// Lấy danh sách phiếu Điều chỉnh giá vốn theo Chi nhánh.
        /// </summary>
        [HttpGet("cost-adjustment")]
        public async Task<IActionResult> GetCostAdjustmentList([FromQuery] long branchId)
        {
            var result = await _documentService.GetCostAdjustmentListAsync(branchId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/cost-adjustment/pending
        /// <summary>
        /// Tạo mới phiếu Điều chỉnh giá vốn ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("cost-adjustment/pending")]
        public async Task<IActionResult> CreateCostAdjustmentPending([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateCostAdjustmentPendingAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/cost-adjustment/completed
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Điều chỉnh giá vốn sang Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("cost-adjustment/completed")]
        public async Task<IActionResult> CreateCostAdjustmentCompleted([FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CreateCostAdjustmentCompletedAsync(document, userId);
            return Ok(result);
        }
        #endregion

        #region PUT api/document/cost-adjustment/pending/{id}
        /// <summary>
        /// Cập nhật phiếu Điều chỉnh giá vốn ở trạng thái Pending.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPut("cost-adjustment/pending/{id}")]
        public async Task<IActionResult> UpdateCostAdjustmentPending(long id, [FromBody] Models.Document document)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.UpdateCostAdjustmentPendingAsync(id, document, userId);
            return Ok(result);
        }
        #endregion

        #region POST api/document/cost-adjustment/{id}/complete
        /// <summary>
        /// Chốt phiếu Điều chỉnh giá vốn Pending -> Completed.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpPost("cost-adjustment/{id}/complete")]
        public async Task<IActionResult> CompleteCostAdjustment(long id)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var result = await _documentService.CompleteCostAdjustmentAsync(id, userId);
            return Ok(result);
        }
        #endregion

        #region DELETE api/document/cost-adjustment/{id}/soft-delete
        /// <summary>
        /// Xóa mềm phiếu Điều chỉnh giá vốn Pending -> Chuyển sang Canceled.
        /// Trích xuất Creator User ID trực tiếp từ Token Access.
        /// </summary>
        [HttpDelete("cost-adjustment/{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteCostAdjustmentPending(long id, [FromQuery] string deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }

            var success = await _documentService.SoftDeleteCostAdjustmentPendingAsync(id, userId, deleteNote);
            return Ok(new { success, message = "Đã chuyển phiếu Điều chỉnh giá vốn sang trạng thái Canceled." });
        }
        #endregion

        #endregion
    }

    /// <summary>
    /// Model dữ liệu cho yêu cầu xử lý nhận hàng / từ chối hàng chuyển kho tại Chi nhánh Nhận.
    /// </summary>
    public class TransferReceiptRequest
    {
        public MenuGoBE.Models.Enums.DocumentTransferStatus Status { get; set; }
        public System.Collections.Generic.List<Models.DocumentDetail>? ReceivedDetails { get; set; }
        public string? Note { get; set; }
    }
}
