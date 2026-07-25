using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Branch;
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

        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats([FromQuery] string range = "thisMonth", [FromQuery] string? startDateStr = null, [FromQuery] string? endDateStr = null)
        {
            try
            {
                var stats = await _service.GetDashboardStatsAsync(range, startDateStr, endDateStr);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Đã xảy ra lỗi khi tính toán số liệu thống kê.",
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
                    error = ex.Message
                });
            }
        }

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
    }
}