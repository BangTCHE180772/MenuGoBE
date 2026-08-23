using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Services.Document;

namespace MenuGoBE.Controllers.Document
{
    /// <summary>
    /// Controller quản lý Sổ Thu Chi (CashFlow) của chi nhánh.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CashFlowController : BaseApiController
    {
        private readonly ICashFlowService _cashFlowService;

        public CashFlowController(ICashFlowService cashFlowService)
        {
            _cashFlowService = cashFlowService;
        }

        #region GET Lấy danh sách thu chi
        /// <summary>
        /// Lấy danh sách phiếu thu chi của chi nhánh (chưa bị xóa mềm), sắp xếp theo BusinessDate giảm dần.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCashFlows([FromQuery] long? branchId)
        {
            if (branchId.HasValue && branchId.Value > 0)
            {
                if (!ValidateUserAndBranchToken(branchId.Value, out _, out var errorResult))
                    return errorResult!;
            }
            else
            {
                if (!ValidateUserToken(out _, out var errorResult))
                    return errorResult!;
            }

            var list = await _cashFlowService.GetCashFlowsAsync(branchId);
            return Ok(list);
        }
        #endregion

        #region GET Lấy chi tiết 1 phiếu
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
                return errorResult!;

            var item = await _cashFlowService.GetByIdAsync(id);
            if (item == null)
                return NotFound(new { message = $"Không tìm thấy phiếu thu chi ID: {id}" });

            return Ok(item);
        }
        #endregion

        #region POST Tạo phiếu thu hoặc chi thủ công
        /// <summary>
        /// Tạo phiếu thu (Inflow) hoặc phiếu chi (Outflow) thủ công.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCashFlowDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Thông tin phiếu không được để trống." });

            if (!ValidateUserAndBranchToken(dto.BranchId, out long userId, out var errorResult))
                return errorResult!;

            try
            {
                var result = await _cashFlowService.CreateAsync(dto, userId);
                return Ok(new
                {
                    success = true,
                    id = result.Id,
                    code = result.Code,
                    message = $"Tạo {(dto.Direction == 1 ? "phiếu thu" : "phiếu chi")} thành công."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion

        #region DELETE Xóa mềm phiếu
        [HttpDelete("{id:long}/soft-delete")]
        public async Task<IActionResult> SoftDelete(long id, [FromQuery] string? deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
                return errorResult!;

            try
            {
                await _cashFlowService.SoftDeleteAsync(id, deleteNote, userId);
                return Ok(new { success = true, message = "Đã hủy phiếu thu chi thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion
    }
}
