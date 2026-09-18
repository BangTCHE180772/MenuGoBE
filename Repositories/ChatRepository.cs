using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly AppDbContext _context;

        public ChatRepository(AppDbContext context)
        {
            _context = context;
        }

        #region GET Lấy cuộc trò chuyện theo Id
        public async Task<Conversation?> GetConversationByIdAsync(long id)
        {
            return await _context.Conversations.FindAsync(id);
        }
        #endregion

        #region GET Lấy cuộc trò chuyện kèm chi tiết quan hệ
        public async Task<Conversation?> GetConversationWithDetailsAsync(long id)
        {
            return await _context.Conversations
                .Include(c => c.Customer)
                .Include(c => c.GuestChatSession)
                .Include(c => c.Branch)
                .Include(c => c.AssignedEmployee)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
        #endregion

        #region GET Lấy danh sách cuộc trò chuyện theo chi nhánh
        public async Task<List<Conversation>> GetConversationsByBranchAsync(long branchId)
        {
            return await _context.Conversations
                .Include(c => c.Customer)
                .Include(c => c.GuestChatSession)
                .Include(c => c.Branch)
                .Include(c => c.AssignedEmployee)
                .Where(c => c.BranchId == branchId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }
        #endregion

        #region GET Lấy danh sách cuộc trò chuyện theo các chi nhánh được phép
        public async Task<List<Conversation>> GetConversationsByAllowedBranchesAsync(IEnumerable<long> branchIds)
        {
            return await _context.Conversations
                .Include(c => c.Customer)
                .Include(c => c.GuestChatSession)
                .Include(c => c.Branch)
                .Include(c => c.AssignedEmployee)
                .Where(c => branchIds.Contains(c.BranchId))
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }
        #endregion

        #region GET Lấy danh sách cuộc trò chuyện của khách hàng đã đăng nhập
        public async Task<List<Conversation>> GetCustomerConversationsAsync(long customerId)
        {
            return await _context.Conversations
                .Include(c => c.Branch)
                .Include(c => c.AssignedEmployee)
                .Where(c => c.CustomerId == customerId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }
        #endregion

        #region GET Lấy danh sách cuộc trò chuyện của khách ẩn danh
        public async Task<List<Conversation>> GetGuestConversationsAsync(long guestSessionId)
        {
            return await _context.Conversations
                .Include(c => c.Branch)
                .Include(c => c.AssignedEmployee)
                .Where(c => c.GuestChatSessionId == guestSessionId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }
        #endregion

        #region GET Lấy cuộc trò chuyện đang hoạt động của khách ẩn danh theo chi nhánh
        public async Task<Conversation?> GetActiveGuestConversationAsync(long guestSessionId, long branchId)
        {
            return await _context.Conversations
                .Include(c => c.Branch)
                .Include(c => c.GuestChatSession)
                .FirstOrDefaultAsync(c => c.GuestChatSessionId == guestSessionId && c.BranchId == branchId);
        }
        #endregion

        #region CREATE Thêm cuộc trò chuyện mới
        public async Task AddConversationAsync(Conversation conversation)
        {
            await _context.Conversations.AddAsync(conversation);
        }
        #endregion

        #region GET Lấy số lượng tin nhắn chưa đọc
        public async Task<int> GetUnreadMessagesCountAsync(long conversationId, bool forCustomer = false)
        {
            if (forCustomer)
            {
                return await _context.Messages
                    .CountAsync(m => m.ConversationId == conversationId && m.EmployeeId.HasValue && !m.ReadAt.HasValue);
            }
            else
            {
                return await _context.Messages
                    .CountAsync(m => m.ConversationId == conversationId && (m.CustomerId.HasValue || m.GuestChatSessionId.HasValue) && !m.ReadAt.HasValue);
            }
        }
        #endregion

        #region GET Lấy danh sách tin nhắn theo cuộc trò chuyện
        public async Task<List<Message>> GetMessagesByConversationIdAsync(long conversationId)
        {
            return await _context.Messages
                .Include(m => m.Customer)
                .Include(m => m.GuestChatSession)
                .Include(m => m.Employee)
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }
        #endregion

        #region CREATE Thêm tin nhắn mới
        public async Task AddMessageAsync(Message message)
        {
            await _context.Messages.AddAsync(message);
        }
        #endregion

        #region CREATE Thêm lịch sử phân công cuộc trò chuyện
        public async Task AddAssignmentHistoryAsync(ConversationAssignmentHistory history)
        {
            await _context.ConversationAssignmentHistories.AddAsync(history);
        }
        #endregion

        #region GET Lấy thông tin phiên khách ẩn danh theo GuestId
        public async Task<GuestChatSession?> GetGuestSessionByGuestIdAsync(Guid guestId)
        {
            return await _context.GuestChatSessions
                .FirstOrDefaultAsync(g => g.GuestId == guestId);
        }
        #endregion

        #region GET Lấy thông tin phiên khách ẩn danh theo Token
        public async Task<GuestChatSession?> GetGuestSessionByTokenAsync(string token)
        {
            return await _context.GuestChatSessions
                .FirstOrDefaultAsync(g => g.Token == token);
        }
        #endregion

        #region CREATE Thêm phiên khách ẩn danh mới
        public async Task AddGuestSessionAsync(GuestChatSession session)
        {
            await _context.GuestChatSessions.AddAsync(session);
        }
        #endregion

        #region DELETE Dọn dẹp các phiên khách ẩn danh đã quá hạn lưu trữ
        public async Task<int> CleanupExpiredGuestSessionsAsync(DateTime cutoffUtc)
        {
            // Tìm các session có LastActiveTime < cutoffUtc
            var expiredSessions = await _context.GuestChatSessions
                .Include(g => g.Conversations)
                .Where(g => g.LastActiveTime < cutoffUtc)
                .ToListAsync();

            if (!expiredSessions.Any()) return 0;

            var sessionIds = expiredSessions.Select(s => s.Id).ToList();
            var convIds = expiredSessions.SelectMany(s => s.Conversations).Select(c => c.Id).ToList();

            if (convIds.Any())
            {
                // Xóa lịch sử phân công và tin nhắn thuộc các cuộc hội thoại hết hạn
                var histories = await _context.ConversationAssignmentHistories
                    .Where(h => convIds.Contains(h.ConversationId))
                    .ToListAsync();
                _context.ConversationAssignmentHistories.RemoveRange(histories);

                var messages = await _context.Messages
                    .Where(m => convIds.Contains(m.ConversationId))
                    .ToListAsync();
                _context.Messages.RemoveRange(messages);

                var conversations = await _context.Conversations
                    .Where(c => convIds.Contains(c.Id))
                    .ToListAsync();
                _context.Conversations.RemoveRange(conversations);
            }

            _context.GuestChatSessions.RemoveRange(expiredSessions);
            await _context.SaveChangesAsync();

            return expiredSessions.Count;
        }
        #endregion

        #region SAVE Lưu các thay đổi vào database
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        #endregion
    }
}
