using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Chat;
using MenuGoBE.Hubs;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    public class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;

        public ChatService(
            IChatRepository chatRepo,
            ICustomerRepository customerRepo,
            IHubContext<NotificationHub> hubContext,
            IMapper mapper,
            AppDbContext context)
        {
            _chatRepo = chatRepo;
            _customerRepo = customerRepo;
            _hubContext = hubContext;
            _mapper = mapper;
            _context = context;
        }

        #region GET Lấy danh sách các cuộc trò chuyện
        public async Task<List<ConversationViewDto>> GetConversationsAsync(
            long currentUserId,
            long? branchId,
            IEnumerable<long> allowedBranchIds,
            bool isAdminOrOwner)
        {
            var allowedBranchList = allowedBranchIds.ToList();
            if (!isAdminOrOwner && !allowedBranchList.Any())
            {
                throw new UnauthorizedAccessException("Tài khoản của bạn chưa được liên kết với bất kỳ chi nhánh nào.");
            }

            if (branchId.HasValue && branchId.Value > 0)
            {
                if (!isAdminOrOwner && !allowedBranchList.Contains(branchId.Value))
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền truy cập chi nhánh này.");
                }
            }

            List<Conversation> list;
            if (branchId.HasValue && branchId.Value > 0)
            {
                list = await _chatRepo.GetConversationsByBranchAsync(branchId.Value);
            }
            else if (!isAdminOrOwner)
            {
                list = await _chatRepo.GetConversationsByAllowedBranchesAsync(allowedBranchList);
            }
            else
            {
                list = await _context.Conversations
                    .Include(c => c.Customer)
                    .Include(c => c.Branch)
                    .Include(c => c.AssignedEmployee)
                    .OrderByDescending(c => c.UpdatedAt)
                    .ToListAsync();
            }

            var dtos = new List<ConversationViewDto>();
            foreach (var c in list)
            {
                var unreadCount = await _chatRepo.GetUnreadMessagesCountAsync(c.Id);
                var dto = _mapper.Map<ConversationViewDto>(c);
                dto.UnreadCount = unreadCount;
                dtos.Add(dto);
            }

            return dtos;
        }
        #endregion

        #region GET Lấy chi tiết cuộc trò chuyện và danh sách tin nhắn
        public async Task<object> GetConversationDetailsAsync(
            long currentUserId,
            long id,
            IEnumerable<long> allowedBranchIds,
            bool isAdminOrOwner,
            bool isCustomer)
        {
            var conversation = await _chatRepo.GetConversationWithDetailsAsync(id);
            if (conversation == null) throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện.");

            if (isCustomer)
            {
                if (conversation.CustomerId != currentUserId)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền truy cập cuộc trò chuyện này.");
                }
            }
            else
            {
                var allowedBranchList = allowedBranchIds.ToList();
                if (!isAdminOrOwner && !allowedBranchList.Contains(conversation.BranchId))
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền truy cập cuộc trò chuyện thuộc chi nhánh này.");
                }

                // Mark unread messages from customer as read
                var unreadMessages = await _context.Messages
                    .Where(m => m.ConversationId == id && m.CustomerId.HasValue && !m.ReadAt.HasValue)
                    .ToListAsync();

                if (unreadMessages.Any())
                {
                    foreach (var m in unreadMessages)
                    {
                        m.ReadAt = DateTime.UtcNow;
                    }
                    await _chatRepo.SaveChangesAsync();
                }
            }

            var messages = await _chatRepo.GetMessagesByConversationIdAsync(id);
            var branchName = conversation.Branch?.Name ?? "Chi nhánh";

            var msgViews = messages.Select(m => new MessageViewDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                CustomerId = m.CustomerId,
                CustomerName = isCustomer 
                    ? (m.CustomerId.HasValue ? (m.Customer?.Name ?? "Khách hàng") : null)
                    : m.Customer?.Name,
                EmployeeId = isCustomer ? null : m.EmployeeId,
                EmployeeName = isCustomer
                    ? (m.EmployeeId.HasValue ? $"MenuGo - Chi nhánh {branchName}" : null)
                    : (m.Employee?.Name ?? $"MenuGo - Chi nhánh {branchName}"),
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                ReadAt = m.ReadAt
            }).ToList();

            var conversationDto = _mapper.Map<ConversationViewDto>(conversation);

            return new
            {
                conversation = conversationDto,
                messages = msgViews
            };
        }
        #endregion

        #region UPDATE Phân công cuộc trò chuyện cho nhân viên
        public async Task<bool> AssignConversationAsync(
            long currentUserId,
            long id,
            long? assignedEmployeeId,
            string? note,
            IEnumerable<long> allowedBranchIds,
            bool isAdminOrOwner,
            bool isManager)
        {
            var conversation = await _chatRepo.GetConversationWithDetailsAsync(id);
            if (conversation == null) throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện.");

            var allowedBranchList = allowedBranchIds.ToList();
            if (!isAdminOrOwner && !allowedBranchList.Contains(conversation.BranchId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên cuộc trò chuyện thuộc chi nhánh này.");
            }

            bool isAssigningToOthers = assignedEmployeeId.HasValue && assignedEmployeeId.Value != currentUserId;
            bool isUnassigning = !assignedEmployeeId.HasValue;

            if ((isAssigningToOthers || isUnassigning) && !isAdminOrOwner && !isManager)
            {
                throw new UnauthorizedAccessException("Nhân viên chỉ có thể tự nhận cuộc trò chuyện cho bản thân (Self-assign).");
            }

            if (assignedEmployeeId.HasValue)
            {
                var employeeId = assignedEmployeeId.Value;
                var hasContractAtBranch = await _context.Contracts
                    .AnyAsync(c => c.AccountId == employeeId && c.BranchId == conversation.BranchId && c.Status == "Active");

                if (!hasContractAtBranch && !isAdminOrOwner)
                {
                    throw new InvalidOperationException("Nhân viên được phân công không thuộc chi nhánh này hoặc hợp đồng không hoạt động.");
                }
            }

            string action = "Assigned";
            if (!conversation.AssignedEmployeeId.HasValue && assignedEmployeeId.HasValue)
            {
                action = "Assigned";
            }
            else if (conversation.AssignedEmployeeId.HasValue && assignedEmployeeId.HasValue)
            {
                action = "Reassigned";
            }
            else if (conversation.AssignedEmployeeId.HasValue && !assignedEmployeeId.HasValue)
            {
                action = "Unassigned";
            }

            conversation.AssignedEmployeeId = assignedEmployeeId;
            conversation.Status = assignedEmployeeId.HasValue ? "Assigned" : "Open";
            conversation.UpdatedAt = DateTime.UtcNow;

            var history = new ConversationAssignmentHistory
            {
                ConversationId = id,
                EmployeeId = assignedEmployeeId,
                Action = action,
                Note = note?.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId
            };

            await _chatRepo.AddAssignmentHistoryAsync(history);
            await _chatRepo.SaveChangesAsync();

            string? empName = null;
            if (assignedEmployeeId.HasValue)
            {
                var emp = await _context.Accounts.FindAsync(assignedEmployeeId.Value);
                empName = emp?.Name;
            }

            var updateDto = _mapper.Map<ConversationViewDto>(conversation);
            updateDto.AssignedEmployeeName = empName;

            // Notify both branch group and customer group
            await _hubContext.Clients.Group($"branch_{conversation.BranchId}").SendAsync("ConversationAssigned", updateDto);
            await _hubContext.Clients.Group($"customer_{conversation.CustomerId}").SendAsync("ConversationAssigned", updateDto);

            return true;
        }
        #endregion

        #region CREATE Gửi tin nhắn từ phía nhân viên
        public async Task<MessageViewDto> SendEmployeeMessageAsync(
            long currentUserId,
            long id,
            string employeeName,
            MessageCreateDto dto,
            IEnumerable<long> allowedBranchIds,
            bool isAdminOrOwner)
        {
            var conversation = await _chatRepo.GetConversationWithDetailsAsync(id);
            if (conversation == null) throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện.");
            if (conversation.Status == "Closed") throw new InvalidOperationException("Cuộc trò chuyện đã đóng.");

            var allowedBranchList = allowedBranchIds.ToList();
            if (!isAdminOrOwner && !allowedBranchList.Contains(conversation.BranchId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền gửi tin nhắn trong cuộc trò chuyện thuộc chi nhánh này.");
            }

            var message = new Message
            {
                ConversationId = id,
                CustomerId = null,
                EmployeeId = currentUserId,
                Content = dto.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            conversation.UpdatedAt = DateTime.UtcNow;

            await _chatRepo.AddMessageAsync(message);
            await _chatRepo.SaveChangesAsync();

            var branchName = conversation.Branch?.Name ?? "Chi nhánh";

            var msgView = new MessageViewDto
            {
                Id = message.Id,
                ConversationId = id,
                CustomerId = null,
                CustomerName = null,
                EmployeeId = currentUserId,
                EmployeeName = employeeName,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            };

            var msgViewForCustomer = new MessageViewDto
            {
                Id = message.Id,
                ConversationId = id,
                CustomerId = null,
                CustomerName = null,
                EmployeeId = null,
                EmployeeName = $"MenuGo - Chi nhánh {branchName}",
                Content = message.Content,
                CreatedAt = message.CreatedAt
            };

            // Notify Branch (detailed) and Customer (anonymous employee)
            await _hubContext.Clients.Group($"branch_{conversation.BranchId}").SendAsync("ReceiveNewMessage", msgView);
            await _hubContext.Clients.Group($"customer_{conversation.CustomerId}").SendAsync("ReceiveNewMessage", msgViewForCustomer);

            // Push a push-style real-time Customer Notification
            var notif = new Notification
            {
                BranchId = conversation.BranchId,
                CustomerId = conversation.CustomerId,
                Type = "Chat",
                Title = $"Tin nhắn mới từ {branchName}",
                Message = dto.Content.Length > 50 ? $"{dto.Content.Substring(0, 47)}..." : dto.Content,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                IsImportant = false,
                Priority = "Information",
                ReferenceType = "Conversation",
                ReferenceId = id
            };
            await _context.Notifications.AddAsync(notif);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"customer_{conversation.CustomerId}").SendAsync("ReceiveNotification", new
            {
                type = "new-message",
                message = $"Bạn có tin nhắn mới từ {branchName}.",
                timestamp = DateTime.UtcNow
            });

            return msgView;
        }
        #endregion

        #region GET Lấy lịch sử phân công cuộc trò chuyện
        public async Task<List<ConversationAssignmentHistoryViewDto>> GetAssignmentHistoryAsync(
            long currentUserId,
            long id,
            IEnumerable<long> allowedBranchIds,
            bool isAdminOrOwner,
            bool isManager)
        {
            var conversation = await _chatRepo.GetConversationByIdAsync(id);
            if (conversation == null) throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện.");

            var allowedBranchList = allowedBranchIds.ToList();
            if (!isAdminOrOwner && !allowedBranchList.Contains(conversation.BranchId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập chi tiết chi nhánh này.");
            }

            if (!isAdminOrOwner && !isManager)
            {
                throw new UnauthorizedAccessException("Chỉ Quản lý hoặc Chủ cửa hàng mới được xem lịch sử phân công.");
            }

            var list = await _context.ConversationAssignmentHistories
                .Include(h => h.Employee)
                .Include(h => h.Creator)
                .Where(h => h.ConversationId == id)
                .OrderBy(h => h.CreatedAt)
                .ToListAsync();

            return list.Select(h => new ConversationAssignmentHistoryViewDto
            {
                Id = h.Id,
                ConversationId = h.ConversationId,
                EmployeeId = h.EmployeeId,
                EmployeeName = h.Employee?.Name,
                Action = h.Action,
                Note = h.Note,
                CreatedAt = h.CreatedAt,
                CreatedBy = h.CreatedBy,
                CreatorName = h.Creator?.Name
            }).ToList();
        }
        #endregion

        #region CREATE Mở hoặc tạo mới cuộc trò chuyện
        public async Task<long> OpenConversationAsync(long customerId, string customerName, ConversationCreateDto dto)
        {
            var conversation = await _context.Conversations
                .Include(c => c.Branch)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.BranchId == dto.BranchId && c.Status != "Closed");

            bool isNew = false;
            if (conversation == null)
            {
                conversation = new Conversation
                {
                    CustomerId = customerId,
                    BranchId = dto.BranchId,
                    Status = "Open",
                    Topic = dto.Topic,
                    Subject = dto.Subject?.Trim(),
                    PreferredContactMethod = dto.PreferredContactMethod,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _chatRepo.AddConversationAsync(conversation);
                await _chatRepo.SaveChangesAsync();
                isNew = true;
            }
            else
            {
                // Re-open if it was open, update the topic/subject if provided
                if (!string.IsNullOrEmpty(dto.Topic) && dto.Topic != "General")
                {
                    conversation.Topic = dto.Topic;
                    conversation.Subject = dto.Subject?.Trim();
                }
                conversation.UpdatedAt = DateTime.UtcNow;
            }

            // Add first message
            var message = new Message
            {
                ConversationId = conversation.Id,
                CustomerId = customerId,
                EmployeeId = null,
                Content = dto.FirstMessageContent.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            await _chatRepo.AddMessageAsync(message);
            await _chatRepo.SaveChangesAsync();

            var branch = await _context.Branches.FindAsync(dto.BranchId);
            var branchName = branch?.Name ?? "Chi nhánh";

            var msgView = new MessageViewDto
            {
                Id = message.Id,
                ConversationId = conversation.Id,
                CustomerId = customerId,
                CustomerName = customerName,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            };

            // Notify Branch via SignalR
            if (isNew)
            {
                var convView = _mapper.Map<ConversationViewDto>(conversation);
                convView.CustomerName = customerName;
                convView.BranchName = branchName;

                await _hubContext.Clients.Group($"branch_{dto.BranchId}").SendAsync("ReceiveNewConversation", convView);
            }
            await _hubContext.Clients.Group($"branch_{dto.BranchId}").SendAsync("ReceiveNewMessage", msgView);

            return conversation.Id;
        }
        #endregion

        #region CREATE Gửi tin nhắn từ phía khách hàng
        public async Task<MessageViewDto> SendCustomerMessageAsync(
            long customerId,
            string customerName,
            long id,
            MessageCreateDto dto)
        {
            var conversation = await _chatRepo.GetConversationWithDetailsAsync(id);
            if (conversation == null) throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện.");
            if (conversation.CustomerId != customerId) throw new UnauthorizedAccessException("Bạn không có quyền gửi tin nhắn trong cuộc trò chuyện này.");
            if (conversation.Status == "Closed") throw new InvalidOperationException("Cuộc trò chuyện đã đóng.");

            var message = new Message
            {
                ConversationId = id,
                CustomerId = customerId,
                EmployeeId = null,
                Content = dto.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            conversation.UpdatedAt = DateTime.UtcNow;

            await _chatRepo.AddMessageAsync(message);
            await _chatRepo.SaveChangesAsync();

            var msgView = new MessageViewDto
            {
                Id = message.Id,
                ConversationId = id,
                CustomerId = customerId,
                CustomerName = customerName,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            };

            // Notify both Branch group and Customer group
            await _hubContext.Clients.Group($"branch_{conversation.BranchId}").SendAsync("ReceiveNewMessage", msgView);
            await _hubContext.Clients.Group($"customer_{customerId}").SendAsync("ReceiveNewMessage", msgView);

            return msgView;
        }
        #endregion

        #region GET Lấy danh sách các cuộc trò chuyện của khách hàng
        public async Task<List<ConversationViewDto>> GetCustomerConversationsAsync(long customerId)
        {
            var list = await _chatRepo.GetCustomerConversationsAsync(customerId);
            return list.Select(c => new ConversationViewDto
            {
                Id = c.Id,
                CustomerId = c.CustomerId,
                CustomerName = c.Customer?.Name ?? "",
                BranchId = c.BranchId,
                BranchName = c.Branch?.Name ?? "",
                Status = c.Status,
                Topic = c.Topic,
                Subject = c.Subject,
                PreferredContactMethod = c.PreferredContactMethod,
                AssignedEmployeeId = c.AssignedEmployeeId,
                AssignedEmployeeName = c.AssignedEmployee?.Name,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                ClosedAt = c.ClosedAt
            }).ToList();
        }
        #endregion
    }
}
