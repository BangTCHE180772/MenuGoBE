using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Notification;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : BaseApiController
    {
        private readonly INotificationService _service;

        public NotificationController(INotificationService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] NotificationQueryDto query)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(new { message = "Không thể trích xuất người dùng." });
            }

            var allowedBranchIds = GetAllowedBranchIdsFromToken();
            if (!allowedBranchIds.Any())
            {
                return Forbid("Tài khoản của bạn chưa được liên kết với bất kỳ chi nhánh nào.");
            }

            if (query.BranchId.HasValue && query.BranchId.Value > 0)
            {
                if (!IsAdmin() && !allowedBranchIds.Contains(query.BranchId.Value))
                {
                    return StatusCode(403, new { message = $"Truy cập bị từ chối: Bạn không có quyền xem thông báo của chi nhánh này. Mã:{query.BranchId.Value}." });
                }
            }

            long[] targetBranchIds;
            if (IsAdmin())
            {
                targetBranchIds = query.BranchId.HasValue && query.BranchId.Value > 0
                    ? new[] { query.BranchId.Value }
                    : Array.Empty<long>();
                if (!query.BranchId.HasValue || query.BranchId.Value <= 0)
                {
                    targetBranchIds = allowedBranchIds;
                }
                else
                {
                    targetBranchIds = new[] { query.BranchId.Value };
                }
            }
            else
            {
                targetBranchIds = query.BranchId.HasValue && query.BranchId.Value > 0
                    ? new[] { query.BranchId.Value }
                    : allowedBranchIds;
            }

            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var result = await _service.GetPagedNotificationsAsync(targetBranchIds, query, userRole, userId);
            return Ok(result);
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentNotifications([FromQuery] long? branchId)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(new { message = "Không thể trích xuất người dùng." });
            }

            var allowedBranchIds = GetAllowedBranchIdsFromToken();
            if (!allowedBranchIds.Any())
            {
                return Forbid("Tài khoản của bạn chưa được liên kết với bất kỳ chi nhánh nào.");
            }

            if (branchId.HasValue && branchId.Value > 0)
            {
                if (!IsAdmin() && !allowedBranchIds.Contains(branchId.Value))
                {
                    return StatusCode(403, new { message = $"Truy cập bị từ chối: Bạn không có quyền xem thông báo của chi nhánh này. Mã:{branchId.Value}." });
                }
            }

            long[] targetBranchIds = branchId.HasValue && branchId.Value > 0
                ? new[] { branchId.Value }
                : allowedBranchIds;

            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var result = await _service.GetRecentNotificationsAsync(targetBranchIds, userRole, userId);
            return Ok(result);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount([FromQuery] long? branchId)
        {
            if (!TryGetUserId(out long userId))
            {
                return Unauthorized(new { message = "Không thể trích xuất người dùng." });
            }

            var allowedBranchIds = GetAllowedBranchIdsFromToken();
            if (!allowedBranchIds.Any())
            {
                return Ok(new { count = 0 });
            }

            if (branchId.HasValue && branchId.Value > 0)
            {
                if (!IsAdmin() && !allowedBranchIds.Contains(branchId.Value))
                {
                    return StatusCode(403, new { message = $"Truy cập bị từ chối: Bạn không có quyền xem thông báo của chi nhánh này. Mã:{branchId.Value}." });
                }
            }

            long[] targetBranchIds = branchId.HasValue && branchId.Value > 0
                ? new[] { branchId.Value }
                : allowedBranchIds;

            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var count = await _service.GetUnreadCountAsync(targetBranchIds, userRole, userId);
            return Ok(new { count });
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            if (!TryGetUserId(out _))
            {
                return Unauthorized(new { message = "Không thể trích xuất người dùng." });
            }

            var allowedBranchIds = GetAllowedBranchIdsFromToken();
            var success = await _service.MarkAsReadAsync(id, allowedBranchIds);
            if (!success)
            {
                return NotFound(new { message = "Không tìm thấy thông báo hoặc bạn không có quyền xem." });
            }

            return Ok(new { success = true, message = "Đã đánh dấu đã đọc thông báo." });
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead([FromQuery] long? branchId)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { message = "Không thể trích xuất người dùng." });
            }

            var allowedBranchIds = GetAllowedBranchIdsFromToken();
            if (!allowedBranchIds.Any() && !IsAdmin())
            {
                return Forbid("Tài khoản của bạn chưa được liên kết với bất kỳ chi nhánh nào.");
            }

            if (branchId.HasValue && branchId.Value > 0)
            {
                if (!IsAdmin() && !allowedBranchIds.Contains(branchId.Value))
                {
                    return StatusCode(403, new { message = $"Truy cập bị từ chối: Bạn không có quyền truy cập thông báo của chi nhánh này. Mã:{branchId.Value}." });
                }
            }

            long[] targetBranchIds = branchId.HasValue && branchId.Value > 0
                ? new[] { branchId.Value }
                : allowedBranchIds;

            var userRole = User.FindFirstValue(ClaimTypes.Role);
            await _service.MarkAllAsReadAsync(targetBranchIds, userRole, userId);
            return Ok(new { success = true, message = "Đã đánh dấu tất cả thông báo là đã đọc." });
        }

        #region Helper Methods
        private long[] GetAllowedBranchIdsFromToken()
        {
            var branchClaims = User.FindAll("branchId").Concat(User.FindAll("BranchId"));
            return branchClaims
                .Select(c => long.TryParse(c.Value, out var bid) ? bid : 0)
                .Where(bid => bid > 0)
                .Distinct()
                .ToArray();
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin") || User.HasClaim(ClaimTypes.Role, "Admin") || User.HasClaim("role", "Admin");
        }
        #endregion
    }
}
