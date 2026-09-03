using MenuGoBE.Hubs;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.AspNetCore.RateLimiting;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerActionController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ITableService _tableService;
        private readonly IOrderAssignmentService _assignmentService;

        public CustomerActionController(
            IHubContext<NotificationHub> hubContext, 
            ITableService tableService,
            IOrderAssignmentService assignmentService)
        {
            _hubContext = hubContext;
            _tableService = tableService;
            _assignmentService = assignmentService;
        }

        private async Task SendTargetedNotificationAsync(long tableId, long branchId, object payload)
        {
            var assignedAccountId = await _assignmentService.GetAssignedAccountByTableAsync(tableId);

            if (assignedAccountId.HasValue)
            {
                await _hubContext.Clients.Group($"waiter_{assignedAccountId.Value}")
                    .SendAsync("ReceiveNotification", payload);
            }
            else
            {
                await _hubContext.Clients.Group($"on_duty_{branchId}")
                    .SendAsync("ReceiveNotification", payload);
            }

            // Manager luôn nhận
            await _hubContext.Clients.Group($"branch_{branchId}")
                .SendAsync("ReceiveNotification", payload);
        }

        [HttpPost("{tableId}/call-staff")]
        [EnableRateLimiting("CustomerActionPolicy")]
        public async Task<IActionResult> CallStaff(long tableId)
        {
            var table = await _tableService.GetByIdAsync(tableId);
            if (table == null) return NotFound("Bàn không tồn tại");

            var payload = new 
            { 
                type = "call-staff", 
                tableId = tableId, 
                tableName = table.Name,
                message = $"Khách ở {table.Name} đang gọi nhân viên!",
                timestamp = System.DateTime.UtcNow
            };

            await SendTargetedNotificationAsync(tableId, table.BranchId, payload);

            return Ok(new { success = true, message = "Đã gọi nhân viên thành công" });
        }

        [HttpPost("{tableId}/request-bill")]
        [EnableRateLimiting("CustomerActionPolicy")]
        public async Task<IActionResult> RequestBill(long tableId)
        {
            var table = await _tableService.GetByIdAsync(tableId);
            if (table == null) return NotFound("Bàn không tồn tại");

            var payload = new 
            { 
                type = "request-bill", 
                tableId = tableId, 
                tableName = table.Name,
                message = $"{table.Name} yêu cầu thanh toán / dọn bàn!",
                timestamp = System.DateTime.UtcNow
            };

            await SendTargetedNotificationAsync(tableId, table.BranchId, payload);

            return Ok(new { success = true, message = "Đã gửi yêu cầu thanh toán" });
        }
    }
}
