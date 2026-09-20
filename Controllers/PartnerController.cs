using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Models.Enums;
using MenuGoBE.Dtos.Partner;
using MenuGoBE.Dtos.Base;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartnerController : BaseApiController
    {
        private readonly IPartnerService _partnerService;

        public PartnerController(IPartnerService partnerService)
        {
            _partnerService = partnerService;
        }

        // 1. GetAll
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PartnerQueryDto query)
        {
            if (query.BranchId.HasValue && query.BranchId.Value > 0)
            {
                if (!ValidateUserAndBranchToken(query.BranchId.Value, out _, out var branchError))
                {
                    return branchError!;
                }
            }
            else
            {
                if (!ValidateUserToken(out _, out var tokenError))
                {
                    return tokenError!;
                }
            }
            var result = await _partnerService.GetPagedPartnersAsync(query);
            return Ok(result);
        }

        // 2. GetByBranchId
        [HttpGet("branch/{branchId}")]
        public async Task<IActionResult> GetByBranchId(
            long branchId,
            [FromQuery] string? search,
            [FromQuery] int? mode)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.GetPartnersByBranchAsync(branchId, search, mode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 2b. GetSuppliers (Mode 1: Supplier)
        [HttpGet("suppliers")]
        public async Task<IActionResult> GetSuppliers(
            [FromQuery] long branchId,
            [FromQuery] string? search)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.GetPartnersByBranchAsync(branchId, search, mode: 1);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 2c. GetExtraPartners (Mode 2: Transporter & Other)
        [HttpGet("extra-partners")]
        public async Task<IActionResult> GetExtraPartners(
            [FromQuery] long branchId,
            [FromQuery] string? search)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.GetPartnersByBranchAsync(branchId, search, mode: 2);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 3. GetByBranchIdAndMode
        [HttpGet("branch/{branchId}/mode/{mode}")]
        public async Task<IActionResult> GetByBranchIdAndMode(
            long branchId,
            int mode,
            [FromQuery] string? search)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.GetPartnersByBranchAsync(branchId, search, mode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #region Lấy thông tin chi tiết đối tác theo ID
        // 3b. GetById
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.GetPartnerByIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion

        // 4. Create (NeedToProvide BranchId)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PartnerCreateDto dto)
        {
            if (!ValidateUserAndBranchToken(dto.BranchId, out long userId, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.CreatePartnerAsync(dto, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 4. Update (Chỉ dựa vào partnerId, không cập nhật BranchId & Type)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] PartnerUpdateDto dto)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.UpdatePartnerAsync(id, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #region Cập nhật hình ảnh hợp đồng cho đối tác
        // 4b. Update Partner Contract Images
        [HttpPut("{id}/images")]
        public async Task<IActionResult> UpdateImages(long id, [FromBody] PartnerImagesUpdateDto dto)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.UpdatePartnerImagesAsync(id, dto?.ImageUrls);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion

        #region Xóa đối tác
        // 5. Delete By Id
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                await _partnerService.DeletePartnerAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion

        #region Lấy tổng quan tài chính công nợ đối tác
        [HttpGet("{id}/financial-summary")]
        public async Task<IActionResult> GetFinancialSummary(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.GetPartnerFinancialSummaryAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion

        #region Lấy danh sách phiếu nhập hàng của đối tác
        [HttpGet("{id}/import-documents")]
        public async Task<IActionResult> GetImportDocuments(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.GetPartnerImportDocumentsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion

        #region Thanh toán tiền cho Nhà cung cấp theo phiếu nhập kho
        [HttpPost("{partnerId}/import-documents/{documentId}/pay")]
        public async Task<IActionResult> PayImportDocument(long partnerId, long documentId, [FromBody] PartnerPayDocumentDto dto)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.PayPartnerImportDocumentAsync(partnerId, documentId, dto, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion

        #region Lấy lịch sử dòng tiền giao dịch tài chính với đối tác
        [HttpGet("{id}/cash-flows")]
        public async Task<IActionResult> GetCashFlows(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.GetPartnerCashFlowHistoryAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion

        #region Lấy lịch sử mua hàng của khách hàng
        [HttpGet("customer/{id}/purchases")]
        public async Task<IActionResult> GetCustomerPurchases(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.GetCustomerPurchaseHistoryAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion

        #region Lấy lịch sử điểm tích lũy của khách hàng
        [HttpGet("customer/{id}/points")]
        public async Task<IActionResult> GetCustomerPoints(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }
            try
            {
                var result = await _partnerService.GetCustomerPointHistoryAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion
    }
}
