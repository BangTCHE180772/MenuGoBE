using MenuGoBE.Dtos.Reservation;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReservationController : ControllerBase
{
    private readonly IReservationService _service;

    public ReservationController(IReservationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReservationCreateDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return Ok(result);
    }

    [HttpPut("{id}/check-in")]
    public async Task<IActionResult> CheckIn(long id)
    {
        var result = await _service.CheckInAsync(id);
        if (!result) return BadRequest(new { message = "Không thể check-in. Bàn có thể đang có khách ngồi hoặc trạng thái không hợp lệ." });
        return Ok(new { message = "Khách đã đến. Bàn đã được mở." });
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(long id)
    {
        var result = await _service.CancelAsync(id);
        if (!result) return BadRequest(new { message = "Không thể hủy đơn đặt bàn này." });
        return Ok(new { message = "Đã hủy đơn đặt bàn thành công." });
    }

    [HttpPut("{id}/change-table")]
    public async Task<IActionResult> ChangeTable(long id, [FromBody] ChangeTableRequest request)
    {
        var result = await _service.ChangeTableAsync(id, request.NewTableId);
        if (!result) return BadRequest(new { message = "Không thể đổi bàn. Bàn dự phòng có thể đã được sử dụng." });
        return Ok(new { message = "Đổi bàn dự phòng thành công." });
    }
}

public class ChangeTableRequest
{
    public long NewTableId { get; set; }
}
