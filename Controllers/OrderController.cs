using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _service;

        public OrderController(IOrderService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy Order" });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] OrderUpdateDto dto)
        {
            var result = await _service.UpdateAsync(dto);

            if (!result)
                return NotFound(new { message = "Không tìm thấy Order để cập nhật" });

            return Ok(new { message = "Cập nhật thành công" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = "Không tìm thấy Order để xóa" });

            return Ok(new { message = "Xóa thành công" });
        }

        [HttpGet("{orderId}/details")]
        public async Task<IActionResult> GetDetails(long orderId)
        {
            var result = await _service.GetDetailsByOrderIdAsync(orderId);
            return Ok(result);
        }

        [HttpGet("{orderId}/details/{id}")]
        public async Task<IActionResult> GetDetailById(long orderId, long id)
        {
            var result = await _service.GetDetailByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy OrderDetail" });

            return Ok(result);
        }

        [HttpPost("{orderId}/details")]
        public async Task<IActionResult> CreateDetail(long orderId, [FromBody] OrderDetailCreateDto dto)
        {
            dto.OrderId = orderId;

            try
            {
                var result = await _service.CreateDetailAsync(dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{orderId}/details/{id}")]
        public async Task<IActionResult> UpdateDetail(long orderId, long id, [FromBody] OrderDetailUpdateDto dto)
        {
            dto.Id = id;

            var result = await _service.UpdateDetailAsync(dto);

            if (!result)
                return NotFound(new { message = "Không tìm thấy OrderDetail để cập nhật" });

            return Ok(new { message = "Cập nhật thành công" });
        }

        [HttpDelete("{orderId}/details/{id}")]
        public async Task<IActionResult> DeleteDetail(long orderId, long id)
        {
            var result = await _service.DeleteDetailAsync(id);

            if (!result)
                return NotFound(new { message = "Không tìm thấy OrderDetail để xóa" });

            return Ok(new { message = "Xóa thành công" });
        }
    }
}
