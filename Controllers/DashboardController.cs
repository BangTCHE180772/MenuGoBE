using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _service;

        public DashboardController(IDashboardService service)
        {
            _service = service;
        }

        [HttpGet("branch-stats")]
        public async Task<IActionResult> GetBranchStats(
            [FromQuery] string range = "thisMonth",
            [FromQuery] string? startDateStr = null,
            [FromQuery] string? endDateStr = null,
            [FromQuery] long? branchId = null)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == "Manager")
                {
                    var branchClaim = User.FindFirst("branchId")?.Value;
                    if (long.TryParse(branchClaim, out var mgrBranchId))
                    {
                        branchId = mgrBranchId;
                    }
                    else
                    {
                        throw new MenuGoException(ErrorCodes.UserBranchIdClaimMissing);
                    }
                }

                var stats = await _service.GetDashboardStatsAsync(range, startDateStr, endDateStr, branchId);
                return Ok(stats);
            }
            catch (MenuGoException ex)
            {
                return StatusCode(ex.HttpStatusCode, new { message = ex.Message, code = ex.Code });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Đã xảy ra lỗi khi tính toán số liệu thống kê dashboard.",
                    error = ex.Message
                });
            }
        }
    }
}
