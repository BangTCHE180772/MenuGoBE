using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Chat;

namespace MenuGoBE.Interface.Services
{
    public interface IChatService
    {
        Task<List<ConversationViewDto>> GetConversationsAsync(long currentUserId, long? branchId, IEnumerable<long> allowedBranchIds, bool isAdminOrOwner);
        Task<object> GetConversationDetailsAsync(long currentUserId, long id, IEnumerable<long> allowedBranchIds, bool isAdminOrOwner, bool isCustomer);
        Task<bool> AssignConversationAsync(long currentUserId, long id, long? assignedEmployeeId, string? note, IEnumerable<long> allowedBranchIds, bool isAdminOrOwner, bool isManager);
        Task<MessageViewDto> SendEmployeeMessageAsync(long currentUserId, long id, string employeeName, MessageCreateDto dto, IEnumerable<long> allowedBranchIds, bool isAdminOrOwner);
        Task<long> OpenConversationAsync(long customerId, string customerName, ConversationCreateDto dto);
        Task<MessageViewDto> SendCustomerMessageAsync(long customerId, string customerName, long id, MessageCreateDto dto);
        Task<List<ConversationViewDto>> GetCustomerConversationsAsync(long customerId);
        Task<List<ConversationAssignmentHistoryViewDto>> GetAssignmentHistoryAsync(long currentUserId, long id, IEnumerable<long> allowedBranchIds, bool isAdminOrOwner, bool isManager);
    }
}
