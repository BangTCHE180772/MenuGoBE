using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Table;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TableController : ControllerBase
    {
        private readonly ITableService _service;

        public TableController(ITableService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // Đọc branchIds từ JWT Claims
            var branchIdClaims = User.Claims
                .Where(c => c.Type == "branchId")
                .Select(c => c.Value)
                .ToList();

            // Nếu người dùng chưa đăng nhập hoặc có branchId thì lọc theo chi nhánh
            if (branchIdClaims.Any())
            {
                var branchIds = branchIdClaims
                    .Select(v => long.TryParse(v, out var id) ? id : 0L)
                    .Where(id => id > 0)
                    .ToList();

                if (branchIds.Any())
                {
                    var filteredResult = await _service.GetByBranchIdsAsync(branchIds);
                    return Ok(filteredResult);
                }
            }

            // Nếu không có branchId trong Token (Admin toàn hệ thống), trả về tất cả
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy Table" });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TableCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] TableUpdateDto dto)
        {
            var result = await _service.UpdateAsync(dto);

            if (!result)
                return NotFound(new { message = "Không tìm thấy Table để cập nhật" });

            return Ok(new { message = "Cập nhật thành công" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = "Không tìm thấy Table để xóa" });

            return Ok(new { message = "Xóa thành công" });
        }
    }
}
