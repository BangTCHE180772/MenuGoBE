using MenuGoBE.Dtos.Reservation;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;

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
    public async Task<IActionResult> GetAll([FromQuery] long? branchId)
    {
        var result = await _service.GetAllAsync();

        if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
        {
            bool isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner") || 
                                  User.HasClaim(ClaimTypes.Role, "Admin") || User.HasClaim(ClaimTypes.Role, "Owner") ||
                                  User.HasClaim("role", "Admin") || User.HasClaim("role", "Owner");
            
            if (!isAdminOrOwner)
            {
                var branchClaims = User.FindAll("branchId").Concat(User.FindAll("BranchId")).Select(c => c.Value).ToList();
                var branchIds = branchClaims.Select(b => long.TryParse(b, out var bid) ? bid : 0L).Where(id => id > 0).ToList();
                
                if (branchIds.Any())
                {
                    var filteredResult = result.Where(x => x.BranchId.HasValue && branchIds.Contains(x.BranchId.Value)).ToList();
                    return Ok(filteredResult);
                }
            }
            else if (branchId.HasValue && branchId.Value > 0)
            {
                // If Admin/Owner explicitly requests a specific branch
                var filteredResult = result.Where(x => x.BranchId == branchId.Value).ToList();
                return Ok(filteredResult);
            }
        }

        return Ok(result);
    }

    [HttpGet("available-tables")]
    public async Task<IActionResult> GetAvailableTables([FromQuery] long branchId, [FromQuery] DateTime reservationTime)
    {
        var result = await _service.GetAvailableTablesAsync(branchId, reservationTime);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReservationCreateDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }
        catch (MenuGoBE.Exceptions.CustomerConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] ReservationUpdateDto dto)
    {
        try
        {
            var result = await _service.UpdateAsync(id, dto);
            if (!result) return NotFound(new { message = "Không tìm thấy thông tin đặt bàn." });
            return Ok(new { message = "Cập nhật thành công." });
        }
        catch (MenuGoBE.Exceptions.CustomerConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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

    [HttpPut("{id}/extend")]
    public async Task<IActionResult> Extend(long id, [FromQuery] int minutes = 30)
    {
        var result = await _service.ExtendAsync(id, minutes);
        if (!result) return BadRequest(new { message = "Không thể gia hạn đơn đặt bàn này." });
        return Ok(new { message = $"Đã gia hạn đơn đặt bàn thêm {minutes} phút thành công." });
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
