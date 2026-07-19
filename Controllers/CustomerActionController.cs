using MenuGoBE.Hubs;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerActionController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ITableService _tableService;

        public CustomerActionController(IHubContext<NotificationHub> hubContext, ITableService tableService)
        {
            _hubContext = hubContext;
            _tableService = tableService;
        }

        [HttpPost("{tableId}/call-staff")]
        public async Task<IActionResult> CallStaff(long tableId)
        {
            var table = await _tableService.GetByIdAsync(tableId);
            if (table == null) return NotFound("Bàn không tồn tại");

            var message = $"Khách ở {table.Name} đang gọi nhân viên!";
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new 
            { 
                type = "call-staff", 
                tableId = tableId, 
                tableName = table.Name,
                message = message,
                timestamp = System.DateTime.UtcNow
            });

            return Ok(new { success = true, message = "Đã gọi nhân viên thành công" });
        }

        [HttpPost("{tableId}/request-bill")]
        public async Task<IActionResult> RequestBill(long tableId)
        {
            var table = await _tableService.GetByIdAsync(tableId);
            if (table == null) return NotFound("Bàn không tồn tại");

            var message = $"Bàn {table.Name} yêu cầu thanh toán / dọn bàn!";
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new 
            { 
                type = "request-bill", 
                tableId = tableId, 
                tableName = table.Name,
                message = message,
                timestamp = System.DateTime.UtcNow
            });

            return Ok(new { success = true, message = "Đã gửi yêu cầu thanh toán" });
        }
    }
}
