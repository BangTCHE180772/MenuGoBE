using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Chat;
using MenuGoBE.Interface.Services;

namespace MenuGoBE.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BranchChatController : BaseApiController
    {
        private readonly IChatService _chatService;
        private readonly AppDbContext _context;

        public BranchChatController(IChatService chatService, AppDbContext context)
        {
            _chatService = chatService;
            _context = context;
        }

        #region Helper Claims Extraction & Shift Validation
        private long[] GetAllowedBranchIds()
        {
            var branchClaims = User.FindAll("branchId").Concat(User.FindAll("BranchId"));
            return branchClaims
                .Select(c => long.TryParse(c.Value, out var bid) ? bid : 0)
                .Where(bid => bid > 0)
                .Distinct()
                .ToArray();
        }

        private bool IsAdminOrOwner()
        {
            return User.IsInRole("Admin") || User.IsInRole("Owner") ||
                    User.HasClaim(ClaimTypes.Role, "Admin") || User.HasClaim(ClaimTypes.Role, "Owner") ||
                    User.HasClaim("role", "Admin") || User.HasClaim("role", "Owner");
        }

        private bool IsManager()
        {
            return User.IsInRole("Manager") || User.HasClaim(ClaimTypes.Role, "Manager") || User.HasClaim("role", "Manager");
        }

        private bool IsCashier()
        {
            return User.IsInRole("Cashier") || User.HasClaim(ClaimTypes.Role, "Cashier") || User.HasClaim("role", "Cashier");
        }

        /// <summary>
        /// Kiểm tra quyền truy cập tính năng chat của nhân viên:
        /// - Admin, Owner và Manager: Luôn có quyền truy cập 24/7.
        /// - Cashier (Thu ngân): Chỉ được truy cập khi đang trong ca làm việc hôm nay (đã check-in, chưa check-out, chưa quá 30 phút sau khi ca kết thúc).
        /// - Các vai trò khác: Không có quyền truy cập chat với khách.
        /// </summary>
        private async Task<(bool Allowed, string Reason)> ValidateStaffChatAccessAsync(long userId, long? targetBranchId)
        {
            // Admin, Owner và Manager được quyền truy cập mọi lúc (24/7)
            if (IsAdminOrOwner() || IsManager())
            {
                return (true, string.Empty);
            }

            // Chỉ cho phép Cashier (Thu ngân), các vai trò khác không được quyền chat với khách
            if (!IsCashier())
            {
                return (false, "Chức năng trò chuyện với khách hàng chỉ dành cho Quản lý, Chủ nhà hàng và Thu ngân đang trong ca làm việc.");
            }

            // Kiểm tra ca làm việc hôm nay của Thu ngân (theo giờ Việt Nam UTC+7)
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var now = DateTime.UtcNow.AddHours(7);

            var query = _context.WorkSchedules
                .Include(ws => ws.Shift)
                .Where(ws => ws.AccountId == userId && ws.WorkDate == today && ws.CheckInAt != null && ws.CheckOutAt == null);

            if (targetBranchId.HasValue && targetBranchId.Value > 0)
            {
                query = query.Where(ws => ws.BranchId == targetBranchId.Value);
            }

            var activeSchedule = await query.FirstOrDefaultAsync();

            if (activeSchedule == null)
            {
                return (false, "Bạn chưa điểm danh (Check-in) ca làm việc hôm nay hoặc đã điểm danh ra về.");
            }

            // Kiểm tra thời gian kết thúc ca + 30 phút gia hạn
            if (activeSchedule.Shift != null)
            {
                var endTime = activeSchedule.Shift.EndTime;
                DateTime shiftEndDateTime;
                if (activeSchedule.Shift.IsNightShift || endTime < activeSchedule.Shift.StartTime)
                {
                    shiftEndDateTime = today.ToDateTime(endTime).AddDays(1);
                }
                else
                {
                    shiftEndDateTime = today.ToDateTime(endTime);
                }

                var disconnectDateTime = shiftEndDateTime.AddMinutes(30);
                if (now > disconnectDateTime)
                {
                    return (false, "Ca làm việc hôm nay của bạn đã kết thúc quá 30 phút. Quyền trò chuyện đã bị ngắt.");
                }
            }

            return (true, string.Empty);
        }
        #endregion

        #region GET Lấy danh sách các cuộc trò chuyện
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations([FromQuery] long? branchId)
        {
            try
            {
                if (!TryGetUserId(out long userId)) return Unauthorized(new { message = "Không thể trích xuất người dùng." });

                var (allowed, reason) = await ValidateStaffChatAccessAsync(userId, branchId);
                if (!allowed)
                {
                    return StatusCode(403, new { message = reason });
                }

                var allowedBranchIds = GetAllowedBranchIds();
                var isAdminOrOwner = IsAdminOrOwner();

                var list = await _chatService.GetConversationsAsync(0, branchId, allowedBranchIds, isAdminOrOwner);
                return Ok(list);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }
        #endregion

        #region GET Lấy chi tiết cuộc trò chuyện và tin nhắn
        [HttpGet("conversations/{id}")]
        public async Task<IActionResult> GetConversationDetails(long id)
        {
            try
            {
                if (!TryGetUserId(out long userId)) return Unauthorized(new { message = "Không thể trích xuất người dùng." });

                var (allowed, reason) = await ValidateStaffChatAccessAsync(userId, null);
                if (!allowed)
                {
                    return StatusCode(403, new { message = reason });
                }

                var allowedBranchIds = GetAllowedBranchIds();
                var isAdminOrOwner = IsAdminOrOwner();

                var details = await _chatService.GetConversationDetailsAsync(0, id, allowedBranchIds, isAdminOrOwner, isCustomer: false);
                return Ok(details);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }
        #endregion

        #region POST Phân công cuộc trò chuyện cho nhân viên
        [HttpPost("conversations/{id}/assign")]
        public async Task<IActionResult> AssignConversation(long id, [FromBody] ConversationAssignRequest request)
        {
            try
            {
                if (!TryGetUserId(out long currentUserId)) return Unauthorized(new { message = "Không thể trích xuất người dùng." });

                var allowedBranchIds = GetAllowedBranchIds();
                var isAdminOrOwner = IsAdminOrOwner();
                var isManager = IsManager();

                await _chatService.AssignConversationAsync(
                    currentUserId,
                    id,
                    request.AssignedEmployeeId,
                    request.Note,
                    allowedBranchIds,
                    isAdminOrOwner,
                    isManager);

                string action = "Assigned";
                return Ok(new { success = true, action = action });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }
        #endregion

        #region POST Gửi tin nhắn từ phía nhân viên
        [HttpPost("conversations/{id}/messages")]
        public async Task<IActionResult> SendMessage(long id, [FromBody] MessageCreateDto dto)
        {
            try
            {
                if (!TryGetUserId(out long currentUserId)) return Unauthorized(new { message = "Không thể trích xuất người dùng." });

                var (allowed, reason) = await ValidateStaffChatAccessAsync(currentUserId, null);
                if (!allowed)
                {
                    return StatusCode(403, new { message = reason });
                }

                var allowedBranchIds = GetAllowedBranchIds();
                var isAdminOrOwner = IsAdminOrOwner();

                var senderName = User.FindFirst("name")?.Value ?? "Nhân viên";

                var msgView = await _chatService.SendEmployeeMessageAsync(
                    currentUserId,
                    id,
                    senderName,
                    dto,
                    allowedBranchIds,
                    isAdminOrOwner);

                return Ok(msgView);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }
        #endregion

        #region GET Lấy lịch sử phân công cuộc trò chuyện
        [HttpGet("conversations/{id}/assignment-history")]
        public async Task<IActionResult> GetAssignmentHistory(long id)
        {
            try
            {
                if (!TryGetUserId(out _)) return Unauthorized(new { message = "Không thể trích xuất người dùng." });

                var allowedBranchIds = GetAllowedBranchIds();
                var isAdminOrOwner = IsAdminOrOwner();
                var isManager = IsManager();

                var list = await _chatService.GetAssignmentHistoryAsync(0, id, allowedBranchIds, isAdminOrOwner, isManager);
                return Ok(list);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }
        #endregion

        #region GET Lấy cấu hình thời gian lưu trữ tin nhắn khách ẩn danh của chi nhánh
        [HttpGet("settings/retention/{branchId}")]
        public async Task<IActionResult> GetRetentionSettings(long branchId)
        {
            try
            {
                if (!TryGetUserId(out _)) return Unauthorized(new { message = "Không thể trích xuất người dùng." });

                var allowedBranchIds = GetAllowedBranchIds();
                var isAdminOrOwner = IsAdminOrOwner();

                if (!isAdminOrOwner && !allowedBranchIds.Contains(branchId))
                {
                    return Forbid("Bạn không có quyền truy cập cấu hình chi nhánh này.");
                }

                var minutes = await _chatService.GetBranchRetentionMinutesAsync(branchId);
                return Ok(new { branchId, retentionMinutes = minutes });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }
        #endregion

        #region PUT Cập nhật cấu hình thời gian lưu trữ tin nhắn khách ẩn danh của chi nhánh
        [HttpPut("settings/retention")]
        public async Task<IActionResult> UpdateRetentionSettings([FromBody] ChatRetentionSettingDto dto)
        {
            try
            {
                if (!TryGetUserId(out _)) return Unauthorized(new { message = "Không thể trích xuất người dùng." });

                var allowedBranchIds = GetAllowedBranchIds();
                var isAdminOrOwner = IsAdminOrOwner();
                var isManager = IsManager();

                await _chatService.UpdateBranchRetentionMinutesAsync(
                    dto.BranchId,
                    dto.RetentionMinutes,
                    allowedBranchIds,
                    isAdminOrOwner,
                    isManager);

                return Ok(new { success = true, message = "Cập nhật thời gian lưu trữ tin nhắn thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }
        #endregion
    }

    public class ConversationAssignRequest
    {
        public long? AssignedEmployeeId { get; set; }
        public string? Note { get; set; }
    }
}
