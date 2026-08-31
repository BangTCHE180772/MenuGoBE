using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Chat;
using MenuGoBE.Interface.Services;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GuestChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public GuestChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        #region Helper Trích xuất Token khách ẩn danh từ Header
        private string GetGuestToken()
        {
            if (Request.Headers.TryGetValue("X-Guest-Token", out var token) && !string.IsNullOrWhiteSpace(token))
            {
                return token.ToString().Trim();
            }

            if (Request.Headers.TryGetValue("Authorization", out var auth) && !string.IsNullOrWhiteSpace(auth))
            {
                var authStr = auth.ToString().Trim();
                if (authStr.StartsWith("Guest ", StringComparison.OrdinalIgnoreCase))
                {
                    return authStr.Substring(6).Trim();
                }
            }

            throw new UnauthorizedAccessException("Không tìm thấy mã xác thực phiên khách ẩn danh (X-Guest-Token).");
        }
        #endregion

        #region POST Khởi tạo hoặc xác thực phiên khách ẩn danh
        [HttpPost("session/init")]
        public async Task<IActionResult> InitSession([FromBody] GuestSessionInitRequestDto dto)
        {
            try
            {
                var session = await _chatService.InitOrValidateGuestSessionAsync(dto);
                return Ok(session);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ khi khởi tạo phiên khách ẩn danh.", error = ex.Message });
            }
        }
        #endregion

        #region GET Lấy danh sách các cuộc trò chuyện của khách ẩn danh
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            try
            {
                var token = GetGuestToken();
                var list = await _chatService.GetGuestConversationsAsync(token);
                return Ok(list);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }
        #endregion

        #region GET Lấy chi tiết cuộc trò chuyện và tin nhắn của khách ẩn danh
        [HttpGet("conversations/{id}")]
        public async Task<IActionResult> GetConversationDetails(long id)
        {
            try
            {
                var token = GetGuestToken();
                var details = await _chatService.GetGuestConversationDetailsAsync(token, id);
                return Ok(details);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
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

        #region POST Gửi tin nhắn từ phía khách ẩn danh
        [HttpPost("conversations/messages")]
        public async Task<IActionResult> SendMessage([FromBody] GuestMessageCreateDto dto)
        {
            try
            {
                var token = GetGuestToken();
                var msgView = await _chatService.SendGuestMessageAsync(token, dto);
                return Ok(msgView);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
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

        #region POST Gửi tin nhắn tư vấn món ăn từ Menu
        [HttpPost("consultation")]
        public async Task<IActionResult> SendProductConsultation([FromBody] ProductConsultationRequestDto dto)
        {
            try
            {
                string? guestToken = null;
                try
                {
                    guestToken = GetGuestToken();
                }
                catch
                {
                    // Cho phép null nếu gọi từ context khác
                }

                var msgView = await _chatService.SendProductConsultationAsync(guestToken, null, null, dto);
                return Ok(msgView);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }
        #endregion
    }
}
