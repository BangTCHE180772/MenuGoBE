using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IChatRepository
    {
        #region Conversation & Message Queries
        Task<Conversation?> GetConversationByIdAsync(long id);
        Task<Conversation?> GetConversationWithDetailsAsync(long id);
        Task<List<Conversation>> GetConversationsByBranchAsync(long branchId);
        Task<List<Conversation>> GetConversationsByAllowedBranchesAsync(IEnumerable<long> branchIds);
        Task<List<Conversation>> GetCustomerConversationsAsync(long customerId);
        Task<List<Conversation>> GetGuestConversationsAsync(long guestSessionId);
        Task<Conversation?> GetActiveGuestConversationAsync(long guestSessionId, long branchId);
        Task AddConversationAsync(Conversation conversation);
        Task<int> GetUnreadMessagesCountAsync(long conversationId, bool forCustomer = false);
        Task<List<Message>> GetMessagesByConversationIdAsync(long conversationId);
        Task AddMessageAsync(Message message);
        Task AddAssignmentHistoryAsync(ConversationAssignmentHistory history);
        #endregion

        #region Guest Session Queries
        Task<GuestChatSession?> GetGuestSessionByGuestIdAsync(Guid guestId);
        Task<GuestChatSession?> GetGuestSessionByTokenAsync(string token);
        Task AddGuestSessionAsync(GuestChatSession session);
        Task<int> CleanupExpiredGuestSessionsAsync(DateTime cutoffUtc);
        #endregion

        #region Persistence
        Task SaveChangesAsync();
        #endregion
    }
}
