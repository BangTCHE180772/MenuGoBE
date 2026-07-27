using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using MenuGoBE.Dtos.SalaryDetail;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SalaryDetailController : ControllerBase
    {
        private readonly ISalaryDetailService _salaryDetailService;
        private readonly IPayrollService _payrollService;
        private readonly IAccountService _accountService;

        public SalaryDetailController(
            ISalaryDetailService salaryDetailService,
            IPayrollService payrollService,
            IAccountService accountService)
        {
            _salaryDetailService = salaryDetailService;
            _payrollService = payrollService;
            _accountService = accountService;
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
        public async Task<IActionResult> GetAll([FromQuery] long? payrollId, [FromQuery] long? accountId)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền xem chi tiết lương." });

            List<long>? roleIdsFilter = isAdmin ? new List<long> { 3 } : new List<long> { 4, 5, 6 };
            List<long>? branchIdsFilter = isAdmin ? null : branchIds;

            var result = await _salaryDetailService.GetAllAsync(payrollId, accountId, branchIdsFilter, roleIdsFilter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền xem chi tiết lương." });

            var detail = await _salaryDetailService.GetByIdAsync(id);
            if (detail == null)
                return NotFound(new { message = $"Không tìm thấy chi tiết lương có ID {id}" });

            var payroll = await _payrollService.GetByIdAsync(detail.PayrollId);
            if (payroll == null)
                return NotFound(new { message = "Không tìm thấy bảng lương tương ứng." });

            var acc = await _accountService.GetByIdAsync(payroll.AccountId);
            if (acc == null)
                return NotFound(new { message = "Không tìm thấy thông tin nhân viên." });

            if (isAdmin)
            {
                if (!acc.RoleIds.Contains(3))
                    return StatusCode(403, new { message = "Chủ sở hữu chỉ được xem chi tiết lương của Quản lý." });
            }
            else
            {
                if (!acc.RoleIds.Exists(r => r == 4 || r == 5 || r == 6))
                    return StatusCode(403, new { message = "Quản lý chỉ được xem chi tiết lương của Nhân viên." });
                if (!branchIds.Contains(payroll.BranchId))
                    return StatusCode(403, new { message = "Bạn không có quyền xem chi tiết lương ở chi nhánh khác." });
            }

            return Ok(detail);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SalaryDetailCreateDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền tạo chi tiết lương." });

            var payroll = await _payrollService.GetByIdAsync(dto.PayrollId);
            if (payroll == null)
                return NotFound(new { message = $"Không tìm thấy bảng lương ID {dto.PayrollId}" });

            var acc = await _accountService.GetByIdAsync(payroll.AccountId);
            if (acc == null)
                return NotFound(new { message = "Không tìm thấy nhân viên của bảng lương này." });

            if (isAdmin)
            {
                if (!acc.RoleIds.Contains(3))
                    return StatusCode(403, new { message = "Chủ sở hữu chỉ được tạo chi tiết lương cho nhân sự Quản lý." });
            }
            else
            {
                if (!acc.RoleIds.Exists(r => r == 4 || r == 5 || r == 6))
                    return StatusCode(403, new { message = "Quản lý chỉ được tạo chi tiết lương cho Nhân viên." });
                if (!branchIds.Contains(payroll.BranchId))
                    return StatusCode(403, new { message = "Bạn không có quyền tạo chi tiết lương thuộc chi nhánh khác." });
            }

            if (dto.CreatedBy <= 0)
            {
                dto.CreatedBy = currentUserId.Value;
            }

            try
            {
                var result = await _salaryDetailService.CreateAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] SalaryDetailUpdateDto dto)
        {
            var currentUserId = GetCurrentAccountId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, branchIds) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Bạn không có quyền sửa chi tiết lương." });

            var detail = await _salaryDetailService.GetByIdAsync(dto.Id);
            if (detail == null)
                return NotFound(new { message = "Không tìm thấy chi tiết lương để cập nhật" });

            var payroll = await _payrollService.GetByIdAsync(detail.PayrollId);
            if (payroll == null)
                return NotFound(new { message = "Không tìm thấy bảng lương tương ứng." });

            var acc = await _accountService.GetByIdAsync(payroll.AccountId);
            if (acc == null)
                return NotFound(new { message = "Không tìm thấy nhân viên của bảng lương này." });

            if (isAdmin)
            {
                if (!acc.RoleIds.Contains(3))
                    return StatusCode(403, new { message = "Chủ sở hữu chỉ được sửa chi tiết lương của Quản lý." });
            }
            else
            {
                if (!acc.RoleIds.Exists(r => r == 4 || r == 5 || r == 6))
                    return StatusCode(403, new { message = "Quản lý chỉ được sửa chi tiết lương của Nhân viên." });
                if (!branchIds.Contains(payroll.BranchId))
                    return StatusCode(403, new { message = "Bạn không có quyền sửa chi tiết lương thuộc chi nhánh khác." });
            }

            try
            {
                var result = await _salaryDetailService.UpdateAsync(dto);
                if (!result)
                    return NotFound(new { message = "Không tìm thấy chi tiết lương để cập nhật" });

                return Ok(new { message = "Cập nhật chi tiết lương thành công" });
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
                return StatusCode(403, new { message = "Bạn không có quyền xóa chi tiết lương." });

            var detail = await _salaryDetailService.GetByIdAsync(id);
            if (detail == null)
                return NotFound(new { message = $"Không tìm thấy chi tiết lương ID {id} để xóa" });

            var payroll = await _payrollService.GetByIdAsync(detail.PayrollId);
            if (payroll == null)
                return NotFound(new { message = "Không tìm thấy bảng lương tương ứng." });

            var acc = await _accountService.GetByIdAsync(payroll.AccountId);
            if (acc == null)
                return NotFound(new { message = "Không tìm thấy nhân viên của bảng lương này." });

            if (isAdmin)
            {
                if (!acc.RoleIds.Contains(3))
                    return StatusCode(403, new { message = "Chủ sở hữu chỉ được xóa chi tiết lương của Quản lý." });
            }
            else
            {
                if (!acc.RoleIds.Exists(r => r == 4 || r == 5 || r == 6))
                    return StatusCode(403, new { message = "Quản lý chỉ được xóa chi tiết lương của Nhân viên." });
                if (!branchIds.Contains(payroll.BranchId))
                    return StatusCode(403, new { message = "Bạn không có quyền xóa chi tiết lương thuộc chi nhánh khác." });
            }

            try
            {
                var result = await _salaryDetailService.DeleteAsync(id);
                if (!result)
                    return NotFound(new { message = $"Không tìm thấy chi tiết lương ID {id} để xóa" });

                return Ok(new { message = "Xóa chi tiết lương thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
