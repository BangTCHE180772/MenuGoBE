using System.Security.Claims;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Contract;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ContractController : ControllerBase
    {
        private readonly IContractService _service;
        private readonly IAccountService _accountService;

        public ContractController(IContractService service, IAccountService accountService)
        {
            _service = service;
            _accountService = accountService;
        }

        private long? GetCurrentAccountId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (claim != null && long.TryParse(claim.Value, out var id))
            {
                return id;
            }
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] long? branchId)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền xem danh sách hợp đồng." });

            if (isAdmin)
            {
                // Owner / Admin only views Manager contracts (RoleId == 3)
                var result = await _service.GetContractsByRolesAsync(new System.Collections.Generic.List<long> { 3 }, branchId);
                return Ok(result);
            }
            else
            {
                // Manager views Staff contracts (RoleIds 4, 5, 6) in their branch(es)
                var result = await _service.GetContractsByRolesAsync(new System.Collections.Generic.List<long> { 4, 5, 6 }, null, branchIds);
                return Ok(result);
            }
        }

        [HttpGet("my-contract")]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyContract()
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var result = await _service.GetByAccountIdAsync(currentUserId.Value);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy Contract" });

            // Allow employee to view their own contract regardless of admin/manager role
            if (result.AccountId == currentUserId.Value)
            {
                return Ok(result);
            }

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền xem hợp đồng." });

            if (isAdmin && result.RoleId != 3)
            {
                return StatusCode(403, new { message = "Owner chỉ có quyền xem hợp đồng của Quản lý." });
            }

            if (!isAdmin && (!branchIds.Contains(result.BranchId) || result.RoleId == 3))
            {
                return StatusCode(403, new { message = "Bạn không có quyền truy cập hợp đồng này." });
            }

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ContractCreateDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền tạo hợp đồng." });

            if (isAdmin)
            {
                // Owner creates Manager contract
                dto.RoleId = 3;
            }
            else
            {
                // Manager creates Staff contract
                if (dto.RoleId == 3)
                {
                    return BadRequest(new { message = "Quản lý không được tạo hợp đồng vai trò Quản lý." });
                }

                if (!branchIds.Contains(dto.BranchId))
                {
                    if (branchIds.Count > 0)
                    {
                        dto.BranchId = branchIds[0];
                    }
                    else
                    {
                        return StatusCode(403, new { message = "Tài khoản quản lý chưa được gán chi nhánh." });
                    }
                }
            }

            try
            {
                var result = await _service.CreateAsync(dto);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ContractUpdateDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền cập nhật hợp đồng." });

            var existing = await _service.GetByIdAsync(dto.Id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy Contract để cập nhật" });

            if (isAdmin)
            {
                if (existing.RoleId != 3)
                {
                    return StatusCode(403, new { message = "Owner chỉ có quyền sửa hợp đồng của Quản lý." });
                }
                dto.RoleId = 3;
            }
            else
            {
                if (!branchIds.Contains(existing.BranchId) || existing.RoleId == 3)
                {
                    return StatusCode(403, new { message = "Bạn không có quyền sửa hợp đồng này." });
                }
                dto.BranchId = existing.BranchId;
            }

            try
            {
                var result = await _service.UpdateAsync(dto);
                if (!result)
                    return NotFound(new { message = "Không tìm thấy Contract để cập nhật" });

                return Ok(new { message = "Cập nhật thành công" });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền xóa hợp đồng." });

            var existing = await _service.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy Contract để xóa" });

            if (isAdmin && existing.RoleId != 3)
            {
                return StatusCode(403, new { message = "Owner chỉ có quyền xóa hợp đồng của Quản lý." });
            }

            if (!isAdmin && (!branchIds.Contains(existing.BranchId) || existing.RoleId == 3))
            {
                return StatusCode(403, new { message = "Bạn không có quyền xóa hợp đồng này." });
            }

            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = "Không tìm thấy Contract để xóa" });

            return Ok(new { message = "Xóa thành công" });
        }
    }
}
