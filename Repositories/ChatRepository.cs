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
                .Include(c => c.Branch)
                .Include(c => c.AssignedEmployee)
                .Where(c => branchIds.Contains(c.BranchId))
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }
        #endregion

        #region GET Lấy danh sách cuộc trò chuyện của khách hàng
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

        #region CREATE Thêm cuộc trò chuyện mới
        public async Task AddConversationAsync(Conversation conversation)
        {
            await _context.Conversations.AddAsync(conversation);
        }
        #endregion

        #region GET Lấy số lượng tin nhắn chưa đọc
        public async Task<int> GetUnreadMessagesCountAsync(long conversationId)
        {
            return await _context.Messages
                .CountAsync(m => m.ConversationId == conversationId && m.CustomerId.HasValue && !m.ReadAt.HasValue);
        }
        #endregion

        #region GET Lấy danh sách tin nhắn theo cuộc trò chuyện
        public async Task<List<Message>> GetMessagesByConversationIdAsync(long conversationId)
        {
            return await _context.Messages
                .Include(m => m.Customer)
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

        #region SAVE Lưu các thay đổi vào database
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        #endregion
    }
}
