using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
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
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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

        #region GET Lấy danh sách các cuộc trò chuyện cho nhân viên / quản lý
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
                    .Include(c => c.GuestChatSession)
                    .Include(c => c.Branch)
                    .Include(c => c.AssignedEmployee)
                    .OrderByDescending(c => c.UpdatedAt)
                    .ToListAsync();
            }

            var dtos = new List<ConversationViewDto>();
            foreach (var c in list)
            {
                var unreadCount = await _chatRepo.GetUnreadMessagesCountAsync(c.Id, forCustomer: false);
                var dto = _mapper.Map<ConversationViewDto>(c);
                dto.UnreadCount = unreadCount;
                
                // Lấy tin nhắn cuối cùng để hiển thị preview
                var lastMsg = await _context.Messages
                    .Where(m => m.ConversationId == c.Id)
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Content)
                    .FirstOrDefaultAsync();
                dto.LastMessage = lastMsg;

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

                // Đánh dấu tin nhắn từ nhân viên là đã đọc đối với khách hàng
                var unreadMessages = await _context.Messages
                    .Where(m => m.ConversationId == id && m.EmployeeId.HasValue && !m.ReadAt.HasValue)
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
            else
            {
                var allowedBranchList = allowedBranchIds.ToList();
                if (!isAdminOrOwner && !allowedBranchList.Contains(conversation.BranchId))
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền truy cập cuộc trò chuyện thuộc chi nhánh này.");
                }

                // Đánh dấu tin nhắn từ khách hàng hoặc khách ẩn danh là đã đọc
                var unreadMessages = await _context.Messages
                    .Where(m => m.ConversationId == id && (m.CustomerId.HasValue || m.GuestChatSessionId.HasValue) && !m.ReadAt.HasValue)
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
                    : (m.Customer?.Name ?? m.GuestChatSession?.GuestName),
                GuestChatSessionId = m.GuestChatSessionId,
                GuestName = m.GuestChatSession?.GuestName,
                EmployeeId = isCustomer ? null : m.EmployeeId,
                EmployeeName = isCustomer
                    ? (m.EmployeeId.HasValue ? $"MenuGo - Chi nhánh {branchName}" : null)
                    : (m.Employee?.Name ?? $"MenuGo - Chi nhánh {branchName}"),
                MessageType = m.MessageType,
                Content = m.Content,
                MetadataJson = m.MetadataJson,
                Topic = m.Topic,
                Subject = m.Subject,
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

            // Thông báo realtime cho nhóm nhân viên chi nhánh và khách hàng
            await _hubContext.Clients.Group($"branch_{conversation.BranchId}").SendAsync("ConversationAssigned", updateDto);
            if (conversation.CustomerId.HasValue)
            {
                await _hubContext.Clients.Group($"customer_{conversation.CustomerId}").SendAsync("ConversationAssigned", updateDto);
            }
            if (conversation.GuestChatSession != null)
            {
                await _hubContext.Clients.Group($"guest_{conversation.GuestChatSession.GuestId}").SendAsync("ConversationAssigned", updateDto);
            }

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

            var allowedBranchList = allowedBranchIds.ToList();
            if (!isAdminOrOwner && !allowedBranchList.Contains(conversation.BranchId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền gửi tin nhắn trong cuộc trò chuyện thuộc chi nhánh này.");
            }

            var message = new Message
            {
                ConversationId = id,
                CustomerId = null,
                GuestChatSessionId = null,
                EmployeeId = currentUserId,
                MessageType = dto.MessageType ?? "Text",
                Content = dto.Content.Trim(),
                MetadataJson = dto.MetadataJson,
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
                GuestChatSessionId = null,
                GuestName = null,
                EmployeeId = currentUserId,
                EmployeeName = employeeName,
                MessageType = message.MessageType,
                Content = message.Content,
                MetadataJson = message.MetadataJson,
                CreatedAt = message.CreatedAt
            };

            var msgViewForCustomer = new MessageViewDto
            {
                Id = message.Id,
                ConversationId = id,
                CustomerId = null,
                CustomerName = null,
                GuestChatSessionId = null,
                GuestName = null,
                EmployeeId = null,
                EmployeeName = $"MenuGo - Chi nhánh {branchName}",
                MessageType = message.MessageType,
                Content = message.Content,
                MetadataJson = message.MetadataJson,
                CreatedAt = message.CreatedAt
            };

            // Gửi realtime cho chi nhánh và khách hàng (hoặc khách ẩn danh)
            await _hubContext.Clients.Group($"branch_{conversation.BranchId}").SendAsync("ReceiveNewMessage", msgView);
            
            if (conversation.CustomerId.HasValue)
            {
                await _hubContext.Clients.Group($"customer_{conversation.CustomerId}").SendAsync("ReceiveNewMessage", msgViewForCustomer);

                // Tạo notification trong hệ thống cho khách hàng đã đăng nhập
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
            }
            else if (conversation.GuestChatSession != null)
            {
                await _hubContext.Clients.Group($"guest_{conversation.GuestChatSession.GuestId}").SendAsync("ReceiveNewMessage", msgViewForCustomer);
            }

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

        #region UPDATE Cập nhật thời gian lưu trữ chat của chi nhánh
        public async Task<bool> UpdateBranchRetentionMinutesAsync(
            long branchId,
            int retentionMinutes,
            IEnumerable<long> allowedBranchIds,
            bool isAdminOrOwner,
            bool isManager)
        {
            var allowedBranchList = allowedBranchIds.ToList();
            if (!isAdminOrOwner && !allowedBranchList.Contains(branchId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền cấu hình chi nhánh này.");
            }

            if (!isAdminOrOwner && !isManager)
            {
                throw new UnauthorizedAccessException("Chỉ Quản lý hoặc Quản trị viên mới được thay đổi cấu hình này.");
            }

            var branch = await _context.Branches.FindAsync(branchId);
            if (branch == null) throw new KeyNotFoundException("Không tìm thấy chi nhánh.");

            branch.AnonymousChatRetentionMinutes = retentionMinutes;
            await _context.SaveChangesAsync();

            return true;
        }
        #endregion

        #region GET Lấy thời gian lưu trữ chat của chi nhánh
        public async Task<int> GetBranchRetentionMinutesAsync(long branchId)
        {
            var branch = await _context.Branches.FindAsync(branchId);
            if (branch == null) throw new KeyNotFoundException("Không tìm thấy chi nhánh.");

            return branch.AnonymousChatRetentionMinutes > 0 ? branch.AnonymousChatRetentionMinutes : 43200;
        }
        #endregion

        #region CREATE Mở hoặc tạo mới cuộc trò chuyện cho khách hàng đã đăng nhập
        public async Task<long> OpenConversationAsync(long customerId, string customerName, ConversationCreateDto dto)
        {
            var conversation = await _context.Conversations
                .Include(c => c.Branch)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.BranchId == dto.BranchId);

            bool isNew = false;
            if (conversation == null)
            {
                conversation = new Conversation
                {
                    CustomerId = customerId,
                    GuestChatSessionId = null,
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
                conversation.Topic = dto.Topic;
                conversation.Subject = dto.Subject?.Trim();
                conversation.UpdatedAt = DateTime.UtcNow;
            }

            var message = new Message
            {
                ConversationId = conversation.Id,
                CustomerId = customerId,
                GuestChatSessionId = null,
                EmployeeId = null,
                MessageType = "Text",
                Content = dto.FirstMessageContent.Trim(),
                Topic = dto.Topic,
                Subject = dto.Subject?.Trim(),
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
                MessageType = message.MessageType,
                Content = message.Content,
                Topic = message.Topic,
                Subject = message.Subject,
                CreatedAt = message.CreatedAt
            };

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

        #region CREATE Gửi tin nhắn từ phía khách hàng đã đăng nhập
        public async Task<MessageViewDto> SendCustomerMessageAsync(
            long customerId,
            string customerName,
            long id,
            MessageCreateDto dto)
        {
            var conversation = await _chatRepo.GetConversationWithDetailsAsync(id);
            if (conversation == null) throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện.");
            if (conversation.CustomerId != customerId) throw new UnauthorizedAccessException("Bạn không có quyền gửi tin nhắn trong cuộc trò chuyện này.");

            var message = new Message
            {
                ConversationId = id,
                CustomerId = customerId,
                GuestChatSessionId = null,
                EmployeeId = null,
                MessageType = dto.MessageType ?? "Text",
                Content = dto.Content.Trim(),
                MetadataJson = dto.MetadataJson,
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
                MessageType = message.MessageType,
                Content = message.Content,
                MetadataJson = message.MetadataJson,
                CreatedAt = message.CreatedAt
            };

            await _hubContext.Clients.Group($"branch_{conversation.BranchId}").SendAsync("ReceiveNewMessage", msgView);
            await _hubContext.Clients.Group($"customer_{customerId}").SendAsync("ReceiveNewMessage", msgView);

            return msgView;
        }
        #endregion

        #region GET Lấy danh sách cuộc trò chuyện của khách hàng đã đăng nhập
        public async Task<List<ConversationViewDto>> GetCustomerConversationsAsync(long customerId)
        {
            var list = await _chatRepo.GetCustomerConversationsAsync(customerId);
            var dtos = new List<ConversationViewDto>();

            foreach (var c in list)
            {
                var unreadCount = await _chatRepo.GetUnreadMessagesCountAsync(c.Id, forCustomer: true);
                var dto = new ConversationViewDto
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
                    ClosedAt = c.ClosedAt,
                    UnreadCount = unreadCount
                };
                dtos.Add(dto);
            }

            return dtos;
        }
        #endregion

        #region GUEST Khởi tạo hoặc xác thực phiên khách ẩn danh
        public async Task<GuestSessionResponseDto> InitOrValidateGuestSessionAsync(GuestSessionInitRequestDto dto)
        {
            GuestChatSession? session = null;

            // 1. Kiểm tra session hiện có bằng Token và GuestId
            if (dto.GuestId.HasValue && !string.IsNullOrEmpty(dto.Token))
            {
                session = await _chatRepo.GetGuestSessionByGuestIdAsync(dto.GuestId.Value);
                if (session != null && session.Token == dto.Token)
                {
                    session.LastActiveTime = DateTime.UtcNow;
                    if (dto.BranchId.HasValue && dto.BranchId.Value > 0)
                    {
                        session.BranchId = dto.BranchId.Value;
                    }
                    await _chatRepo.SaveChangesAsync();

                    return new GuestSessionResponseDto
                    {
                        GuestId = session.GuestId,
                        GuestName = session.GuestName,
                        Token = session.Token,
                        CreatedAt = session.CreatedAt,
                        LastActiveTime = session.LastActiveTime
                    };
                }
            }

            // 2. Tạo phiên khách ẩn danh mới
            var randomCode = RandomNumberGenerator.GetInt32(1000, 9999);
            var secureToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLower();

            session = new GuestChatSession
            {
                GuestId = Guid.NewGuid(),
                GuestName = $"Khách hàng #{randomCode}",
                Token = secureToken,
                CreatedAt = DateTime.UtcNow,
                LastActiveTime = DateTime.UtcNow,
                BranchId = dto.BranchId
            };

            await _chatRepo.AddGuestSessionAsync(session);
            await _chatRepo.SaveChangesAsync();

            return new GuestSessionResponseDto
            {
                GuestId = session.GuestId,
                GuestName = session.GuestName,
                Token = session.Token,
                CreatedAt = session.CreatedAt,
                LastActiveTime = session.LastActiveTime
            };
        }
        #endregion

        #region GUEST Lấy danh sách các cuộc trò chuyện của khách ẩn danh
        public async Task<List<ConversationViewDto>> GetGuestConversationsAsync(string guestToken)
        {
            var session = await _chatRepo.GetGuestSessionByTokenAsync(guestToken);
            if (session == null) throw new UnauthorizedAccessException("Phiên khách ẩn danh không hợp lệ hoặc đã hết hạn.");

            session.LastActiveTime = DateTime.UtcNow;
            await _chatRepo.SaveChangesAsync();

            var list = await _chatRepo.GetGuestConversationsAsync(session.Id);
            var dtos = new List<ConversationViewDto>();

            foreach (var c in list)
            {
                var unreadCount = await _chatRepo.GetUnreadMessagesCountAsync(c.Id, forCustomer: true);
                var dto = new ConversationViewDto
                {
                    Id = c.Id,
                    GuestChatSessionId = session.Id,
                    CustomerName = session.GuestName,
                    GuestName = session.GuestName,
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
                    ClosedAt = c.ClosedAt,
                    UnreadCount = unreadCount
                };
                dtos.Add(dto);
            }

            return dtos;
        }
        #endregion

        #region GUEST Lấy chi tiết cuộc trò chuyện của khách ẩn danh
        public async Task<object> GetGuestConversationDetailsAsync(string guestToken, long conversationId)
        {
            var session = await _chatRepo.GetGuestSessionByTokenAsync(guestToken);
            if (session == null) throw new UnauthorizedAccessException("Phiên khách ẩn danh không hợp lệ hoặc đã hết hạn.");

            var conversation = await _chatRepo.GetConversationWithDetailsAsync(conversationId);
            if (conversation == null) throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện.");
            if (conversation.GuestChatSessionId != session.Id)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập cuộc trò chuyện này.");
            }

            session.LastActiveTime = DateTime.UtcNow;

            // Đánh dấu tin nhắn từ nhân viên là đã đọc
            var unreadEmployeeMsgs = await _context.Messages
                .Where(m => m.ConversationId == conversationId && m.EmployeeId.HasValue && !m.ReadAt.HasValue)
                .ToListAsync();
            foreach (var m in unreadEmployeeMsgs)
            {
                m.ReadAt = DateTime.UtcNow;
            }
            await _chatRepo.SaveChangesAsync();

            var messages = await _chatRepo.GetMessagesByConversationIdAsync(conversationId);
            var branchName = conversation.Branch?.Name ?? "Chi nhánh";

            var msgViews = messages.Select(m => new MessageViewDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                CustomerId = null,
                CustomerName = null,
                GuestChatSessionId = m.GuestChatSessionId,
                GuestName = m.GuestChatSessionId.HasValue ? session.GuestName : null,
                EmployeeId = null,
                EmployeeName = m.EmployeeId.HasValue ? $"MenuGo - Chi nhánh {branchName}" : null,
                MessageType = m.MessageType,
                Content = m.Content,
                MetadataJson = m.MetadataJson,
                Topic = m.Topic,
                Subject = m.Subject,
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

        #region GUEST Gửi tin nhắn từ phía khách ẩn danh
        public async Task<MessageViewDto> SendGuestMessageAsync(string guestToken, GuestMessageCreateDto dto)
        {
            var session = await _chatRepo.GetGuestSessionByTokenAsync(guestToken);
            if (session == null) throw new UnauthorizedAccessException("Phiên khách ẩn danh không hợp lệ hoặc đã hết hạn.");

            session.LastActiveTime = DateTime.UtcNow;

            // 1. Tìm hoặc mở cuộc trò chuyện với chi nhánh
            Conversation? conversation = null;
            if (dto.ConversationId.HasValue && dto.ConversationId.Value > 0)
            {
                conversation = await _chatRepo.GetConversationWithDetailsAsync(dto.ConversationId.Value);
            }

            if (conversation == null || conversation.GuestChatSessionId != session.Id)
            {
                conversation = await _chatRepo.GetActiveGuestConversationAsync(session.Id, dto.BranchId);
            }

            bool isNew = false;
            if (conversation == null)
            {
                conversation = new Conversation
                {
                    GuestChatSessionId = session.Id,
                    CustomerId = null,
                    BranchId = dto.BranchId,
                    Status = "Open",
                    Topic = dto.Topic ?? "General",
                    Subject = dto.Subject?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _chatRepo.AddConversationAsync(conversation);
                await _chatRepo.SaveChangesAsync();
                isNew = true;
            }
            else
            {
                conversation.UpdatedAt = DateTime.UtcNow;
            }

            // 2. Xử lý tin nhắn và Metadata món ăn nếu có
            string metadataJson = null!;
            string messageContent = dto.Content?.Trim() ?? string.Empty;

            if (dto.MessageType == "ProductReference" && dto.ProductIds != null && dto.ProductIds.Any())
            {
                var products = await _context.Products
                    .Include(p => p.Group)
                    .Include(p => p.Image)
                    .Where(p => dto.ProductIds.Contains(p.Id))
                    .ToListAsync();

                var productItems = products.Select(p => new ProductReferenceItemDto
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Price = p.SellPrice,
                    ImageLink = p.Image?.ImageLink ?? "",
                    GroupName = p.Group?.Name ?? ""
                }).ToList();

                metadataJson = JsonSerializer.Serialize(new { products = productItems }, _jsonOptions);
                if (string.IsNullOrEmpty(messageContent))
                {
                    messageContent = $"Yêu cầu tư vấn {productItems.Count} món ăn";
                }
            }

            var message = new Message
            {
                ConversationId = conversation.Id,
                GuestChatSessionId = session.Id,
                CustomerId = null,
                EmployeeId = null,
                MessageType = dto.MessageType ?? "Text",
                Content = messageContent,
                MetadataJson = metadataJson,
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
                GuestChatSessionId = session.Id,
                CustomerName = session.GuestName,
                GuestName = session.GuestName,
                MessageType = message.MessageType,
                Content = message.Content,
                MetadataJson = message.MetadataJson,
                CreatedAt = message.CreatedAt
            };

            // Thông báo realtime cho nhóm nhân viên chi nhánh và group của khách ẩn danh
            if (isNew)
            {
                var convView = _mapper.Map<ConversationViewDto>(conversation);
                convView.CustomerName = session.GuestName;
                convView.GuestName = session.GuestName;
                convView.BranchName = branchName;

                await _hubContext.Clients.Group($"branch_{dto.BranchId}").SendAsync("ReceiveNewConversation", convView);
            }
            await _hubContext.Clients.Group($"branch_{dto.BranchId}").SendAsync("ReceiveNewMessage", msgView);
            await _hubContext.Clients.Group($"guest_{session.GuestId}").SendAsync("ReceiveNewMessage", msgView);

            return msgView;
        }
        #endregion

        #region CONSULTATION Gửi tin nhắn tư vấn món ăn từ Menu
        public async Task<MessageViewDto> SendProductConsultationAsync(
            string? guestToken,
            long? customerId,
            string? customerName,
            ProductConsultationRequestDto dto)
        {
            if (!dto.ProductIds.Any())
            {
                throw new ArgumentException("Vui lòng chọn ít nhất một món ăn để tư vấn.");
            }

            // Nạp thông tin sản phẩm chuẩn từ cơ sở dữ liệu
            var products = await _context.Products
                .Include(p => p.Group)
                .Include(p => p.Image)
                .Where(p => dto.ProductIds.Contains(p.Id))
                .ToListAsync();

            if (!products.Any())
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin món ăn yêu cầu.");
            }

            var productItems = products.Select(p => new ProductReferenceItemDto
            {
                ProductId = p.Id,
                Name = p.Name,
                Price = p.SellPrice,
                ImageLink = p.Image?.ImageLink ?? "",
                GroupName = p.Group?.Name ?? ""
            }).ToList();

            var metadataJson = JsonSerializer.Serialize(new
            {
                products = productItems,
                note = dto.Note?.Trim()
            }, _jsonOptions);

            var content = dto.ProductIds.Count == 1
                ? $"Tư vấn món: {productItems[0].Name}"
                : $"Yêu cầu tư vấn danh sách ({productItems.Count}) món ăn";

            if (customerId.HasValue)
            {
                // Khách hàng đã đăng nhập
                var conv = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.CustomerId == customerId.Value && c.BranchId == dto.BranchId);

                bool isNew = false;
                if (conv == null)
                {
                    conv = new Conversation
                    {
                        CustomerId = customerId.Value,
                        BranchId = dto.BranchId,
                        Status = "Open",
                        Topic = "Menu",
                        Subject = "Tư vấn món ăn",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _chatRepo.AddConversationAsync(conv);
                    await _chatRepo.SaveChangesAsync();
                    isNew = true;
                }

                var msg = new Message
                {
                    ConversationId = conv.Id,
                    CustomerId = customerId.Value,
                    MessageType = "ProductReference",
                    Content = content,
                    MetadataJson = metadataJson,
                    CreatedAt = DateTime.UtcNow
                };
                conv.UpdatedAt = DateTime.UtcNow;

                await _chatRepo.AddMessageAsync(msg);
                await _chatRepo.SaveChangesAsync();

                var msgDto = new MessageViewDto
                {
                    Id = msg.Id,
                    ConversationId = conv.Id,
                    CustomerId = customerId,
                    CustomerName = customerName,
                    MessageType = msg.MessageType,
                    Content = msg.Content,
                    MetadataJson = msg.MetadataJson,
                    CreatedAt = msg.CreatedAt
                };

                if (isNew)
                {
                    var branch = await _context.Branches.FindAsync(dto.BranchId);
                    var convView = _mapper.Map<ConversationViewDto>(conv);
                    convView.CustomerName = customerName;
                    convView.BranchName = branch?.Name ?? "";
                    await _hubContext.Clients.Group($"branch_{dto.BranchId}").SendAsync("ReceiveNewConversation", convView);
                }

                await _hubContext.Clients.Group($"branch_{dto.BranchId}").SendAsync("ReceiveNewMessage", msgDto);
                await _hubContext.Clients.Group($"customer_{customerId}").SendAsync("ReceiveNewMessage", msgDto);

                return msgDto;
            }
            else
            {
                // Khách ẩn danh
                if (string.IsNullOrEmpty(guestToken))
                {
                    throw new UnauthorizedAccessException("Phiên khách ẩn danh không hợp lệ.");
                }

                var guestMsgDto = new GuestMessageCreateDto
                {
                    BranchId = dto.BranchId,
                    ConversationId = dto.ConversationId,
                    Content = content,
                    MessageType = "ProductReference",
                    ProductIds = dto.ProductIds,
                    Topic = "Menu",
                    Subject = "Tư vấn món ăn"
                };

                return await SendGuestMessageAsync(guestToken, guestMsgDto);
            }
        }
        #endregion
    }
}
