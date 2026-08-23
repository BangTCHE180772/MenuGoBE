using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IChatRepository
    {

        Task<Conversation?> GetConversationByIdAsync(long id);
        Task<Conversation?> GetConversationWithDetailsAsync(long id);
        Task<List<Conversation>> GetConversationsByBranchAsync(long branchId);
        Task<List<Conversation>> GetConversationsByAllowedBranchesAsync(IEnumerable<long> branchIds);
        Task<List<Conversation>> GetCustomerConversationsAsync(long customerId);
        Task AddConversationAsync(Conversation conversation);
        Task<int> GetUnreadMessagesCountAsync(long conversationId);
        Task<List<Message>> GetMessagesByConversationIdAsync(long conversationId);
        Task AddMessageAsync(Message message);
        Task AddAssignmentHistoryAsync(ConversationAssignmentHistory history);
        Task SaveChangesAsync();
    }
}
