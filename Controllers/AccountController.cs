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
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _service;

        public AccountController(IAccountService service)
        {
            _service = service;
        }

        private long? GetCurrentAccountId()
        {
            var sub = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                   ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("id")?.Value;

            if (long.TryParse(sub, out var accountId))
                return accountId;

            return null;
        }

        [HttpGet("role-range")]
        public async Task<IActionResult> GetAllAccountsRoleRange([FromQuery] long? branchId)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _service.GetAccountPermissionsAsync(currentUserId.Value);
            
            List<long>? filterBranchIds = null;
            if (branchId.HasValue)
            {
                if (!isAdmin && !branchIds.Contains(branchId.Value))
                {
                    return StatusCode(403, new { message = "Bạn không có quyền xem tài khoản của chi nhánh này." });
                }
                filterBranchIds = new List<long> { branchId.Value };
            }
            else
            {
                filterBranchIds = isAdmin ? null : branchIds; // Admin gets all, others get their branches
            }

            var result = await _service.GetAccountsExcludeRolesAsync(new List<long>(), filterBranchIds, currentUserId.Value);
            return Ok(result);
        }

        [HttpGet("exclude-roles")]
        public async Task<IActionResult> GetAccountsExcludeRoles([FromQuery] long? branchId)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _service.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền quản lý tài khoản." });

            if (isAdmin)
            {
                // Owner / Admin ONLY sees Manager accounts (Role 3), optionally filtered by branchId
                var result = await _service.GetAccountsByRolesAsync(new List<long> { 3 }, branchId);
                return Ok(result);
            }
            else // isManager
            {
                List<long>? filterBranchIds = null;
                if (branchId.HasValue)
                {
                    if (!branchIds.Contains(branchId.Value))
                    {
                        return StatusCode(403, new { message = "Bạn không có quyền xem tài khoản của chi nhánh này." });
                    }
                    filterBranchIds = new List<long> { branchId.Value };
                }
                else
                {
                    filterBranchIds = branchIds;
                }

                // Manager ONLY sees staff accounts (excluding role 1, 2 and 3)
                var result = await _service.GetAccountsExcludeRolesAsync(new List<long> { 1, 2, 3 }, filterBranchIds, currentUserId.Value);
                return Ok(result);
            }
        }

        [HttpGet("chefs")]
        public async Task<IActionResult> GetChefs([FromQuery] long? branchId)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _service.GetAccountPermissionsAsync(currentUserId.Value);

            long? targetBranchId = null;
            if (branchId.HasValue)
            {
                if (!isAdmin && !branchIds.Contains(branchId.Value))
                {
                    return StatusCode(403, new { message = "Bạn không thuộc chi nhánh này." });
                }
                targetBranchId = branchId.Value;
            }
            else
            {
                targetBranchId = !isAdmin && branchIds.Any() ? branchIds.First() : null;
            }

            var result = await _service.GetAccountsByRolesAsync(new List<long> { 5 }, targetBranchId);
            return Ok(result);
        }

        [HttpGet("managers")]
        public async Task<IActionResult> GetManagerAccounts()
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, _, _) = await _service.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin)
                return StatusCode(403, new { message = "Chỉ có Owner / Admin mới có quyền xem danh sách Quản lý." });

            // Fetch accounts with role 3 (Quản lý)
            var result = await _service.GetAccountsByRolesAsync(new List<long> { 3 });
            return Ok(result);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var result = await _service.GetByIdAsync(currentUserId.Value);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy Account" });

            return Ok(result);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] AccountUpdateDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            // Always update the caller's own account, regardless of whatever Id was sent in the body.
            dto.Id = currentUserId.Value;

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

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            try
            {
                await _service.ChangePasswordAsync(currentUserId.Value, dto.CurrentPassword, dto.NewPassword);
                return Ok(new { message = "Đổi mật khẩu thành công" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _service.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền quản lý tài khoản." });

            if (!isAdmin && isManager)
            {
                bool canAccess = await _service.CanManagerAccessAccountAsync(id, branchIds, currentUserId.Value);
                if (!canAccess)
                    return StatusCode(403, new { message = "Bạn không có quyền xem tài khoản thuộc chi nhánh khác." });
            }

            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy Account" });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AccountCreateDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, _) = await _service.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền tạo tài khoản." });

            try
            {
                var result = await _service.CreateAsync(dto, currentUserId.Value);
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
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _service.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền cập nhật tài khoản." });

            if (!isAdmin && isManager)
            {
                bool canAccess = await _service.CanManagerAccessAccountAsync(dto.Id, branchIds, currentUserId.Value);
                if (!canAccess)
                    return StatusCode(403, new { message = "Bạn không có quyền cập nhật tài khoản thuộc chi nhánh khác." });
            }

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
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _service.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền xóa tài khoản." });

            if (!isAdmin && isManager)
            {
                bool canAccess = await _service.CanManagerAccessAccountAsync(id, branchIds, currentUserId.Value);
                if (!canAccess)
                    return StatusCode(403, new { message = "Bạn không có quyền xóa tài khoản thuộc chi nhánh khác." });
            }

            try
            {
                var result = await _service.DeleteAsync(id);
                if (!result)
                    return NotFound(new { message = "Không tìm thấy Account để xóa" });

                return Ok(new { message = "Xóa thành công" });
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
