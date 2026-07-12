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
        private readonly IOrderDetailService _detailService;

        public OrderController(IOrderService service, IOrderDetailService detailService)
        {
            _service = service;
            _detailService = detailService;
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

        [HttpPost("merge")]
        public async Task<IActionResult> Merge([FromBody] OrderMergeDto dto)
        {
            var result = await _service.MergeAsync(dto);

            if (!result)
                return BadRequest(new { message = "Không thể gộp đơn. Kiểm tra lại OrderId hoặc đơn cha đã bị gộp vào đơn khác." });

            return Ok(new { message = "Gộp đơn thành công" });
        }

        [HttpDelete("{id}/unmerge")]
        public async Task<IActionResult> Unmerge(long id)
        {
            var result = await _service.UnmergeAsync(id);

            if (!result)
                return BadRequest(new { message = "Không thể tách đơn. Đơn này chưa được gộp." });

            return Ok(new { message = "Tách đơn thành công" });
        }

        [HttpGet("{fatherId}/children")]
        public async Task<IActionResult> GetChildren(long fatherId)
        {
            var result = await _service.GetChildOrdersAsync(fatherId);
            return Ok(result);
        }

        [HttpGet("{fatherId}/merged-summary")]
        public async Task<IActionResult> GetMergedSummary(long fatherId)
        {
            var result = await _service.GetMergedSummaryAsync(fatherId);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy đơn hàng" });

            return Ok(result);
        }

        [HttpGet("{orderId}/details")]
        public async Task<IActionResult> GetDetails(long orderId)
        {
            var result = await _detailService.GetByOrderIdAsync(orderId);
            return Ok(result);
        }

        [HttpGet("{orderId}/details/{id}")]
        public async Task<IActionResult> GetDetailById(long orderId, long id)
        {
            var result = await _detailService.GetByIdAsync(id);

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
                var result = await _detailService.CreateAsync(dto);
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

            var result = await _detailService.UpdateAsync(dto);

            if (!result)
                return NotFound(new { message = "Không tìm thấy OrderDetail để cập nhật" });

            return Ok(new { message = "Cập nhật thành công" });
        }

        [HttpDelete("{orderId}/details/{id}")]
        public async Task<IActionResult> DeleteDetail(long orderId, long id)
        {
            var result = await _detailService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = "Không tìm thấy OrderDetail để xóa" });

            return Ok(new { message = "Xóa thành công" });
        }

        [HttpGet("by-table/{tableId}")]
        public async Task<IActionResult> GetTableOrderSummary(long tableId)
        {
            var summary = await _service.GetTableOrderSummaryAsync(tableId);
            return Ok(summary);
        }

        [HttpPut("details/{id}/confirm")]
        public async Task<IActionResult> ConfirmOrderDetail(long id)
        {
            var result = await _detailService.ConfirmAsync(id);
            if (!result)
                return NotFound(new { message = "Không tìm thấy chi tiết đơn hoặc cập nhật thất bại" });

            return Ok(new { message = "Xác nhận thành công" });
        }

        [HttpPut("details/{id}/cooking-status")]
        public async Task<IActionResult> UpdateCookingStatus(long id, [FromBody] UpdateCookingStatusDto dto)
        {
            var result = await _detailService.UpdateCookingStatusAsync(id, dto.CookingStatus);
            if (!result)
                return NotFound(new { message = "Không tìm thấy chi tiết đơn hoặc cập nhật thất bại" });

            return Ok(new { message = "Cập nhật trạng thái thành công" });
        }
    }
}
