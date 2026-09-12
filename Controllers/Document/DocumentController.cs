using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models.Enums;

using Microsoft.EntityFrameworkCore;
using MenuGoBE.Data;

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
        private readonly IDocumentService _documentService;
        private readonly AppDbContext _context;

        public DocumentController(IDocumentService documentService, AppDbContext context)
        {
            _documentService = documentService;
            _context = context;
        }

        #region Tra cứu thông tin điều hướng theo mã chứng từ / mã tham chiếu
        /// <summary>
        /// Tra cứu thông tin và đường dẫn chi tiết tương ứng theo mã chứng từ hoặc mã đối tượng (NH..., TH..., XH..., CK..., KK..., DC..., HD..., DT..., KH...).
        /// </summary>
        [HttpGet("lookup")]
        public async Task<IActionResult> LookupByCode([FromQuery] string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { message = "Mã không được để trống" });
            }

            var cleanCode = code.Trim();

            // 1. Tìm trong Documents
            var doc = await _context.Documents
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Code == cleanCode);

            if (doc != null)
            {
                string url;
                switch (doc.Type)
                {
                    case DocumentType.Import:
                        url = $"/import-detail/{doc.Id}";
                        break;
                    case DocumentType.Return:
                        url = $"/import-return-detail/{doc.Id}";
                        break;
                    case DocumentType.ExportDelete:
                        url = $"/export-delete-detail/{doc.Id}";
                        break;
                    case DocumentType.Transfer:
                        url = $"/transfer-detail/{doc.Id}";
                        break;
                    case DocumentType.Check:
                        url = $"/check-detail/{doc.Id}";
                        break;
                    case DocumentType.CostAdjustment:
                        url = $"/adjustment-detail/{doc.Id}";
                        break;
                    case DocumentType.Sale:
                        url = $"/invoice-detail/{doc.OrderId ?? doc.Id}";
                        break;
                    case DocumentType.Production:
                        url = $"/production-detail/{doc.Id}";
                        break;
                    default:
                        url = $"/import-detail/{doc.Id}";
                        break;
                }

                return Ok(new
                {
                    found = true,
                    type = doc.Type.ToString(),
                    id = doc.Id,
                    code = doc.Code,
                    url
                });
            }

            // 2. Tìm trong Orders (Hóa đơn HD...)
            if (cleanCode.StartsWith("HD", StringComparison.OrdinalIgnoreCase))
            {
                var numStr = System.Text.RegularExpressions.Regex.Replace(cleanCode, @"\D", "");
                if (long.TryParse(numStr, out long orderId))
                {
                    var orderExists = await _context.Orders.AnyAsync(o => o.Id == orderId);
                    if (orderExists)
                    {
                        return Ok(new
                        {
                            found = true,
                            type = "Invoice",
                            id = orderId,
                            code = $"HD{orderId}",
                            url = $"/invoice-detail/{orderId}"
                        });
                    }
                }
            }

            // 3. Tìm trong Partners (Đối tác / NCC DT...)
            if (cleanCode.StartsWith("DT", StringComparison.OrdinalIgnoreCase) || cleanCode.StartsWith("NCC", StringComparison.OrdinalIgnoreCase))
            {
                var numStr = System.Text.RegularExpressions.Regex.Replace(cleanCode, @"\D", "");
                if (long.TryParse(numStr, out long partnerId))
                {
                    var partnerExists = await _context.Partners.AnyAsync(p => p.Id == partnerId);
                    if (partnerExists)
                    {
                        return Ok(new
                        {
                            found = true,
                            type = "Partner",
                            id = partnerId,
                            code = $"DT{partnerId}",
                            url = $"/partner-detail/{partnerId}"
                        });
                    }
                }
            }

            // 4. Tìm trong Customers (Khách hàng KH...)
            if (cleanCode.StartsWith("KH", StringComparison.OrdinalIgnoreCase))
            {
                var numStr = System.Text.RegularExpressions.Regex.Replace(cleanCode, @"\D", "");
                if (long.TryParse(numStr, out long customerId))
                {
                    var customerExists = await _context.Customers.AnyAsync(c => c.Id == customerId);
                    if (customerExists)
                    {
                        return Ok(new
                        {
                            found = true,
                            type = "Customer",
                            id = customerId,
                            code = $"KH{customerId}",
                            url = $"/customer-detail/{customerId}"
                        });
                    }
                }
            }

            // 5. Tìm trong BInventories / Products
            var binv = await _context.BInventories
                .Include(b => b.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Product.SKUCode == cleanCode);

            if (binv != null)
            {
                return Ok(new
                {
                    found = true,
                    type = "Product",
                    id = binv.Id,
                    code = binv.Product.SKUCode,
                    url = $"/inventory-management?tab=Product&search={Uri.EscapeDataString(binv.Product.SKUCode ?? cleanCode)}&productId={binv.ProductId}&inventoryId={binv.Id}&expand=true"
                });
            }

            return Ok(new { found = false });
        }
        #endregion

        #region Generic Read APIs (API GET Dùng chung cho Tất cả Loại Chứng từ)
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
