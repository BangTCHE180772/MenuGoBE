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
            var result = await _service.GetAllAsync();
            return Ok(result);
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BranchCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] BranchUpdateDto dto)
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