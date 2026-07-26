using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;
using MenuGoBE.Dtos.Account;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _service;

        public AccountController(IAccountService service)
        {
            _service = service;
        }

        [HttpGet("exclude-roles")]
        public async Task<IActionResult> GetAccountsExcludeRoles([FromQuery] long? branchId = null)
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
                    return BadRequest(new { message = "Tài khoản quản lý không có thông tin chi nhánh liên kết hợp lệ." });
                }
            }

            // Fetch accounts excluding role 1, 2 and 3
            var result = await _service.GetAccountsExcludeRolesAsync(new List<long> { 1, 2, 3 }, branchId);
            return Ok(result);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy Account" });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AccountCreateDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] AccountUpdateDto dto)
        {
            try
            {
                var result = await _service.UpdateAsync(dto);
                if (!result)
                    return NotFound(new { message = "Không tìm thấy Account để cập nhật" });

                return Ok(new { message = "Cập nhật thành công" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = "Không tìm thấy Account để xóa" });

            return Ok(new { message = "Xóa thành công" });
        }
    }
}
