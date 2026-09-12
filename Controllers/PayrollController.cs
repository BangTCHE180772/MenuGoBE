using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Payroll;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PayrollController : ControllerBase
    {
        private readonly IPayrollService _payrollService;
        private readonly IAccountService _accountService;
        private readonly IPayrollSuggestionService _suggestionService;

        public PayrollController(
            IPayrollService payrollService,
            IAccountService accountService,
            IPayrollSuggestionService suggestionService)
        {
            _payrollService = payrollService;
            _accountService = accountService;
            _suggestionService = suggestionService;
        }

        private long? GetCurrentAccountId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("id");
            if (claim != null && long.TryParse(claim.Value, out var id))
            {
                return id;
            }
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] long? branchId, [FromQuery] int? month, [FromQuery] int? year, [FromQuery] long? accountId)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền quản lý bảng lương." });

            List<long> allowedRoleIds;
            long? targetBranchId = branchId;

            if (isAdmin)
            {
                // Owner / Admin can view & manage payrolls of all employee roles (Managers & Regular Staff)
                allowedRoleIds = null;
            }
            else
            {
                // Manager can ONLY view / manage Payrolls of staff employees (Roles 4, 5, 6)
                allowedRoleIds = new List<long> { 4, 5, 6 };

                if (branchId.HasValue && branchId.Value > 0)
                {
                    if (!branchIds.Contains(branchId.Value))
                    {
                        return StatusCode(403, new { message = "Bạn không có quyền xem bảng lương thuộc chi nhánh khác." });
                    }
                }
                else
                {
                    if (branchIds.Count > 0)
                    {
                        targetBranchId = branchIds[0];
                    }
                    else
                    {
                        return StatusCode(403, new { message = "Tài khoản quản lý chưa được gán chi nhánh." });
                    }
                }
            }

            var result = await _payrollService.GetAllAsync(targetBranchId, month, year, accountId, allowedRoleIds);
            return Ok(result);
        }

        [HttpGet("my-payroll")]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyPayroll([FromQuery] int? month, [FromQuery] int? year)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var result = await _payrollService.GetByAccountIdAsync(currentUserId.Value, month, year);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var result = await _payrollService.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = $"Không tìm thấy bảng lương có ID {id}" });

            // Allow employee to view their own payroll details
            if (result.AccountId == currentUserId.Value)
            {
                return Ok(result);
            }

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền xem bảng lương." });

            var acc = await _accountService.GetByIdAsync(result.AccountId);
            if (acc == null)
                return NotFound(new { message = "Không tìm thấy thông tin nhân viên của bảng lương này." });

            if (isAdmin)
            {
                // Owner / Admin has full access to view any employee's payroll
            }
            else
            {
                if (!acc.RoleIds.Exists(r => r == 4 || r == 5 || r == 6))
                {
                    return StatusCode(403, new { message = "Quản lý chỉ được quản lý bảng lương của nhân viên (Thu ngân, Bếp, Phục vụ)." });
                }
                if (!branchIds.Contains(result.BranchId))
                {
                    return StatusCode(403, new { message = "Bạn không có quyền truy cập bảng lương thuộc chi nhánh khác." });
                }
            }

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PayrollCreateDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền tạo bảng lương." });

            var targetAcc = await _accountService.GetByIdAsync(dto.AccountId);
            if (targetAcc == null)
                return BadRequest(new { message = "Không tìm thấy thông tin tài khoản nhân viên được chọn." });

            if (isAdmin && !isManager)
            {
                if (!targetAcc.RoleIds.Exists(r => r == 3))
                {
                    return StatusCode(403, new { message = "Chủ sở hữu chỉ được phép tạo bảng lương cho Quản lý. Bảng lương nhân viên do Quản lý tạo." });
                }
            }
            else
            {
                if (!targetAcc.RoleIds.Exists(r => r == 4 || r == 5 || r == 6))
                {
                    return StatusCode(403, new { message = "Quản lý chỉ được tạo bảng lương cho nhân viên (Thu ngân, Bếp, Phục vụ)." });
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

            if (dto.CreatedBy <= 0)
            {
                dto.CreatedBy = currentUserId.Value;
            }

            try
            {
                var result = await _payrollService.CreateAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] PayrollGenerateDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền tự động tính bảng lương." });

            var targetAcc = await _accountService.GetByIdAsync(dto.AccountId);
            if (targetAcc == null)
                return BadRequest(new { message = "Không tìm thấy thông tin tài khoản nhân viên được chọn." });

            if (isAdmin && !isManager)
            {
                if (!targetAcc.RoleIds.Exists(r => r == 3))
                {
                    return StatusCode(403, new { message = "Chủ sở hữu chỉ được phép tự động tính bảng lương cho Quản lý. Bảng lương nhân viên do Quản lý tính." });
                }
            }
            else
            {
                if (!targetAcc.RoleIds.Exists(r => r == 4 || r == 5 || r == 6))
                {
                    return StatusCode(403, new { message = "Quản lý chỉ được tự động tính bảng lương cho nhân viên (Thu ngân, Bếp, Phục vụ)." });
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

            if (dto.CreatedBy <= 0)
            {
                dto.CreatedBy = currentUserId.Value;
            }

            try
            {
                var result = await _payrollService.GenerateEmployeePayrollAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("generate-batch")]
        public async Task<IActionResult> GenerateBatch([FromBody] PayrollBatchGenerateDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền tự động tính bảng lương hàng loạt." });

            if (dto.CreatedBy <= 0)
            {
                dto.CreatedBy = currentUserId.Value;
            }

            try
            {
                var result = await _payrollService.GenerateBatchPayrollAsync(dto, isAdmin, isManager, branchIds);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] PayrollUpdateDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền cập nhật bảng lương." });

            var existing = await _payrollService.GetByIdAsync(dto.Id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy bảng lương để cập nhật" });

            var acc = await _accountService.GetByIdAsync(existing.AccountId);
            if (acc == null)
                return NotFound(new { message = "Không tìm thấy thông tin nhân viên của bảng lương này." });

            if (isAdmin && !isManager)
            {
                if (!acc.RoleIds.Exists(r => r == 3))
                {
                    return StatusCode(403, new { message = "Chủ sở hữu chỉ được phép chỉnh sửa bảng lương của Quản lý." });
                }
            }
            else
            {
                if (!acc.RoleIds.Exists(r => r == 4 || r == 5 || r == 6))
                {
                    return StatusCode(403, new { message = "Quản lý chỉ được cập nhật bảng lương của nhân viên (Thu ngân, Bếp, Phục vụ)." });
                }
                if (!branchIds.Contains(existing.BranchId))
                {
                    return StatusCode(403, new { message = "Bạn không có quyền sửa bảng lương thuộc chi nhánh khác." });
                }
            }

            try
            {
                var result = await _payrollService.UpdateAsync(dto);
                if (!result)
                    return NotFound(new { message = "Không tìm thấy bảng lương để cập nhật" });

                return Ok(new { message = "Cập nhật bảng lương thành công" });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = msg });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(long id, [FromQuery] PayrollStatus status, [FromQuery] DateTime? paymentDate)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền cập nhật trạng thái bảng lương." });

            var existing = await _payrollService.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Không tìm thấy bảng lương ID {id}" });

            var acc = await _accountService.GetByIdAsync(existing.AccountId);
            if (acc == null)
                return NotFound(new { message = "Không tìm thấy thông tin nhân viên của bảng lương này." });

            if (isAdmin)
            {
                if (!acc.RoleIds.Contains(3))
                {
                    return StatusCode(403, new { message = "Chủ sở hữu chỉ được cập nhật trạng thái bảng lương của nhân sự có vai trò Quản lý." });
                }
            }
            else
            {
                if (!acc.RoleIds.Exists(r => r == 4 || r == 5 || r == 6))
                {
                    return StatusCode(403, new { message = "Quản lý chỉ được cập nhật trạng thái bảng lương của nhân viên (Thu ngân, Bếp, Phục vụ)." });
                }
                if (!branchIds.Contains(existing.BranchId))
                {
                    return StatusCode(403, new { message = "Bạn không có quyền cập nhật trạng thái bảng lương thuộc chi nhánh khác." });
                }
            }

            try
            {
                var result = await _payrollService.UpdateStatusAsync(id, status, paymentDate);
                if (!result)
                    return NotFound(new { message = $"Không tìm thấy bảng lương ID {id}" });

                return Ok(new { message = $"Cập nhật trạng thái bảng lương thành {status} thành công" });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = msg });
            }
        }

        [HttpPost("{id}/lock")]
        public async Task<IActionResult> LockPayroll(long id)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, _) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền chốt (khoá) bảng lương." });

            try
            {
                var success = await _payrollService.LockPayrollAsync(id, currentUserId.Value);
                if (!success) return NotFound(new { message = $"Không tìm thấy bảng lương ID {id}" });
                return Ok(new { message = "Đã chốt (khóa) bảng lương thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/unlock")]
        public async Task<IActionResult> UnlockPayroll(long id)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, _) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền mở khóa bảng lương." });

            try
            {
                var success = await _payrollService.UnlockPayrollAsync(id, currentUserId.Value);
                if (!success) return NotFound(new { message = $"Không tìm thấy bảng lương ID {id}" });
                return Ok(new { message = "Đã mở khóa bảng lương thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApprovePayroll(long id)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, _, _) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin)
                return StatusCode(403, new { message = "Chỉ Owner hoặc Admin mới có quyền phê duyệt bảng lương." });

            try
            {
                var success = await _payrollService.ApprovePayrollAsync(id, currentUserId.Value);
                if (!success) return NotFound(new { message = $"Không tìm thấy bảng lương ID {id}" });
                return Ok(new { message = "Đã phê duyệt bảng lương thành công." });
            }
            catch (Exception ex)
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
                return StatusCode(403, new { message = "Bạn không có quyền xóa bảng lương." });

            var existing = await _payrollService.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Không tìm thấy bảng lương ID {id} để xóa" });

            var acc = await _accountService.GetByIdAsync(existing.AccountId);
            if (acc == null)
                return NotFound(new { message = "Không tìm thấy thông tin nhân viên của bảng lương này." });

            if (isAdmin && !isManager)
            {
                if (!acc.RoleIds.Exists(r => r == 3))
                {
                    return StatusCode(403, new { message = "Chủ sở hữu chỉ được phép xóa bảng lương của Quản lý." });
                }
            }
            else
            {
                if (!acc.RoleIds.Exists(r => r == 4 || r == 5 || r == 6))
                {
                    return StatusCode(403, new { message = "Quản lý chỉ được xóa bảng lương của nhân viên (Thu ngân, Bếp, Phục vụ)." });
                }
                if (!branchIds.Contains(existing.BranchId))
                {
                    return StatusCode(403, new { message = "Bạn không có quyền xóa bảng lương thuộc chi nhánh khác." });
                }
            }

            try
            {
                var result = await _payrollService.DeleteAsync(id);
                if (!result)
                    return NotFound(new { message = $"Không tìm thấy bảng lương ID {id} để xóa" });

                return Ok(new { message = "Xóa bảng lương thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #region Kiến nghị bảng lương (Payroll Suggestions)

        [HttpPost("suggestions")]
        public async Task<IActionResult> CreateSuggestion([FromBody] PayrollSuggestionCreateDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            try
            {
                var result = await _suggestionService.CreateAsync(currentUserId.Value, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions(
            [FromQuery] long? branchId,
            [FromQuery] long? accountId,
            [FromQuery] int? month,
            [FromQuery] int? year,
            [FromQuery] string? status)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var query = new PayrollSuggestionQueryDto
            {
                BranchId = branchId,
                AccountId = accountId,
                Month = month,
                Year = year,
                Status = status
            };

            var (isAdmin, isManager, userBranchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
            {
                // Employees can only view their own suggestions
                query.AccountId = currentUserId.Value;
            }
            else if (isManager && !isAdmin && (!query.BranchId.HasValue || query.BranchId.Value <= 0))
            {
                // Managers default filter to their assigned branches
                if (userBranchIds.Any())
                {
                    query.BranchId = userBranchIds.First();
                }
            }

            var result = await _suggestionService.GetFilteredAsync(query);
            return Ok(result);
        }

        [HttpGet("suggestions/{id}")]
        public async Task<IActionResult> GetSuggestionById(long id)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var result = await _suggestionService.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = $"Không tìm thấy kiến nghị lương ID {id}" });

            return Ok(result);
        }

        [HttpPut("suggestions/{id}/process")]
        public async Task<IActionResult> ProcessSuggestion(long id, [FromBody] PayrollSuggestionProcessDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, _) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Chỉ Quản lý hoặc Chủ cửa hàng mới có quyền duyệt kiến nghị lương." });

            try
            {
                var result = await _suggestionService.ProcessAsync(id, currentUserId.Value, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("suggestions/{id}")]
        public async Task<IActionResult> UpdateSuggestion(long id, [FromBody] PayrollSuggestionUpdateDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            try
            {
                var result = await _suggestionService.UpdateAsync(id, currentUserId.Value, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("suggestions/{id}")]
        public async Task<IActionResult> DeleteSuggestion(long id)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            try
            {
                await _suggestionService.DeleteAsync(id, currentUserId.Value);
                return Ok(new { message = "Xóa kiến nghị lương thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion
    }
}
