using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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

        public BranchChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        #region Helper Claims Extraction
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
        #endregion

        #region GET Lấy danh sách các cuộc trò chuyện
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations([FromQuery] long? branchId)
        {
            try
            {
                if (!TryGetUserId(out _)) return Unauthorized(new { message = "Không thể trích xuất người dùng." });

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
                if (!TryGetUserId(out _)) return Unauthorized(new { message = "Không thể trích xuất người dùng." });

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
    }

    public class ConversationAssignRequest
    {
        public long? AssignedEmployeeId { get; set; }
        public string? Note { get; set; }
    }
}
