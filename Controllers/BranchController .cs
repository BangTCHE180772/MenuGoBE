using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Branch;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BranchController : ControllerBase
    {
        private readonly IBranchService _service;

        public BranchController(IBranchService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _service.GetAllAsync();

                if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
                {
                    bool isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner") || 
                                          User.HasClaim(ClaimTypes.Role, "Admin") || User.HasClaim(ClaimTypes.Role, "Owner") ||
                                          User.HasClaim("role", "Admin") || User.HasClaim("role", "Owner");
                                          
                    bool isManager = User.IsInRole("Manager") || User.HasClaim(ClaimTypes.Role, "Manager") || User.HasClaim("role", "Manager");

                    if (isManager && !isAdminOrOwner)
                    {
                        var branchClaims = User.FindAll("branchId").Concat(User.FindAll("BranchId")).Select(c => c.Value).ToList();
                        result = result.Where(b => branchClaims.Contains(b.Id.ToString())).ToList();
                    }
                }

                //200
                return Ok(result);
            }
            catch (Exception ex)
            {
                //500
                return StatusCode(500, new
                {
                    message = "Đã xảy ra lỗi khi lấy danh sách chi nhánh.",
                    error = ex.Message
                });
            }
        }

        #region GET Lấy danh sách các chi nhánh nhận hàng đang hoạt động (dùng cho chuyển kho)
        [HttpGet("transfer-destinations")]
        public async Task<IActionResult> GetTransferDestinations()
        {
            try
            {
                var result = await _service.GetAllAsync();
                var activeBranches = result
                    .Where(b => !b.IsDeleted && b.Status == "Hoạt động")
                    .ToList();

                return Ok(activeBranches);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Đã xảy ra lỗi khi lấy danh sách chi nhánh nhận hàng.",
                    error = ex.Message
                });
            }
        }
        #endregion

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound(new
                {
                    message = $"Không tìm thấy Branch  = {id}"
                });

            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] BranchQueryDto dto)
        {
            try
            {
                var result = await _service.SearchFilteredBranchesAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Đã xảy ra lỗi khi lấy danh sách chi nhánh.",
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BranchCreateDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto);
                //201
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Id },
                    result);
            }
            catch (Exception ex)
            {
                return StatusCode(400, new
                {
                    message = "Đã xảy ra lỗi khi thêm chi nhánh.",
                    error = ex.Message
                });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] BranchUpdateDto dto)
        {

            try
            {
                var success = await _service.UpdateAsync(dto);

                if (!success)
                    return NotFound(new
                    {
                        message = $"Không tìm thấy Branch có Id = {dto.Id}"
                    });

                return Ok(new
                {
                    message = "Cập nhật Branch thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new
                {
                    message = "Đã xảy ra lỗi khi sửa chi nhánh.",
                    error = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message
                });
            }
        }

        #region DELETE Xóa mềm chi nhánh
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var success = await _service.DeleteAsync(id);

            if (!success)
                return NotFound(new
                {
                    message = $"Không tìm thấy Branch có Id = {id}"
                });

            return Ok(new
            {
                message = "Xóa Branch thành công"
            });
        }
        #endregion

        #region DELETE Xóa vĩnh viễn (Hard Delete / Purge) sạch sẽ một chi nhánh và toàn bộ dữ liệu liên quan (Chỉ dành cho BE)
        [HttpDelete("hard-delete/{id:long}")]
        public async Task<IActionResult> HardDelete(long id)
        {
            try
            {
                var success = await _service.HardDeleteBranchAsync(id);

                if (!success)
                    return NotFound(new
                    {
                        success = false,
                        message = $"Không tìm thấy Branch có Id = {id} để xóa vĩnh viễn."
                    });

                return Ok(new
                {
                    success = true,
                    message = $"Đã xóa sạch sẽ toàn bộ dữ liệu của chi nhánh Id = {id}."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi xóa sạch chi nhánh.",
                    error = ex.Message
                });
            }
        }
        #endregion
    }
}