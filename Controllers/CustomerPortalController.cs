using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Chat;
using MenuGoBE.Dtos.Reservation;
using MenuGoBE.Interface.Services;

namespace MenuGoBE.Controllers
{
    [Authorize(Roles = "Customer")]
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerPortalController : ControllerBase
    {
        private readonly ICustomerPortalService _portalService;
        private readonly IChatService _chatService;

        public CustomerPortalController(
            ICustomerPortalService portalService,
            IChatService chatService)
        {
            _portalService = portalService;
            _chatService = chatService;
        }

        #region Helper Methods
        private long GetCustomerId()
        {
            var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(subClaim) || !long.TryParse(subClaim, out var customerId))
            {
                throw new UnauthorizedAccessException("Không thể xác định danh tính khách hàng.");
            }
            return customerId;
        }
        #endregion

        #region GET Lấy thông tin hồ sơ khách hàng
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var customerId = GetCustomerId();
                var profile = await _portalService.GetProfileAsync(customerId);
                return Ok(profile);
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

        #region PUT Cập nhật thông tin hồ sơ khách hàng
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] MenuGoBE.Dtos.Auth.CustomerUpdateProfileDto dto)
        {
            try
            {
                var customerId = GetCustomerId();
                var updated = await _portalService.UpdateProfileAsync(customerId, dto);
                return Ok(new { success = true, message = "Cập nhật thông tin thành công.", customer = updated });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
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

        #region GET Lấy danh sách lịch đặt bàn của khách hàng
        [HttpGet("reservations")]
        public async Task<IActionResult> GetMyReservations()
        {
            try
            {
                var customerId = GetCustomerId();
                var list = await _portalService.GetMyReservationsAsync(customerId);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }
        #endregion

        #region POST Tạo mới lịch đặt bàn
        [HttpPost("reservations")]
        public async Task<IActionResult> CreateReservation([FromBody] ReservationCreateDto dto)
        {
            try
            {
                var customerId = GetCustomerId();
                var result = await _portalService.CreateReservationAsync(customerId, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion

        #region PUT Hủy lịch đặt bàn
        [HttpPut("reservations/{id}/cancel")]
        public async Task<IActionResult> CancelReservation(long id)
        {
            try
            {
                var customerId = GetCustomerId();
                var success = await _portalService.CancelReservationAsync(customerId, id);
                if (!success) return BadRequest(new { message = "Hủy lịch đặt bàn thất bại." });

                return Ok(new { success = true, message = "Đã hủy lịch đặt bàn thành công." });
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

        #region GET Lấy danh sách thông báo của khách hàng
        [HttpGet("notifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            try
            {
                var customerId = GetCustomerId();
                var list = await _portalService.GetMyNotificationsAsync(customerId);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }
        #endregion

        #region GET Lấy danh sách các cuộc trò chuyện của khách hàng
        [HttpGet("conversations")]
        public async Task<IActionResult> GetMyConversations()
        {
            try
            {
                var customerId = GetCustomerId();
                var list = await _chatService.GetCustomerConversationsAsync(customerId);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }
        #endregion

        #region POST Mở hoặc tạo mới cuộc trò chuyện
        [HttpPost("conversations")]
        public async Task<IActionResult> OpenConversation([FromBody] ConversationCreateDto dto)
        {
            try
            {
                var customerId = GetCustomerId();
                var customer = await _portalService.GetProfileAsync(customerId);

                var conversationId = await _chatService.OpenConversationAsync(customerId, customer.Name, dto);
                return Ok(new { success = true, conversationId = conversationId, id = conversationId });
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

        #region GET Lấy chi tiết cuộc trò chuyện và tin nhắn của khách hàng
        [HttpGet("conversations/{id}")]
        public async Task<IActionResult> GetConversationDetails(long id)
        {
            try
            {
                var customerId = GetCustomerId();
                var details = await _chatService.GetConversationDetailsAsync(customerId, id, null, false, isCustomer: true);
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

        #region POST Gửi tin nhắn từ phía khách hàng
        [HttpPost("conversations/{id}/messages")]
        public async Task<IActionResult> SendMessage(long id, [FromBody] MessageCreateDto dto)
        {
            try
            {
                var customerId = GetCustomerId();
                var customer = await _portalService.GetProfileAsync(customerId);

                var msgView = await _chatService.SendCustomerMessageAsync(customerId, customer.Name, id, dto);
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

        #region POST Gửi yêu cầu tư vấn món ăn từ Menu
        [HttpPost("consultation")]
        public async Task<IActionResult> SendProductConsultation([FromBody] ProductConsultationRequestDto dto)
        {
            try
            {
                var customerId = GetCustomerId();
                var customer = await _portalService.GetProfileAsync(customerId);

                var msgView = await _chatService.SendProductConsultationAsync(null, customerId, customer.Name, dto);
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
