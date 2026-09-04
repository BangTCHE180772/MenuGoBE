using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MenuGoBE.Dtos.Chat;

#region Request DTOs
public class ConversationCreateDto
{
    [Required(ErrorMessage = "Chi nhánh là bắt buộc.")]
    public long BranchId { get; set; }

    [Required(ErrorMessage = "Chủ đề là bắt buộc.")]
    public string Topic { get; set; } = "General"; // General, Booking, Menu, Event, Pricing, Allergy, Other

    [MaxLength(255)]
    public string? Subject { get; set; }

    [MaxLength(50)]
    public string? PreferredContactMethod { get; set; } // Chat, Phone, Email

    [Required(ErrorMessage = "Tin nhắn đầu tiên là bắt buộc.")]
    public string FirstMessageContent { get; set; } = string.Empty;
}

public class MessageCreateDto
{
    [Required(ErrorMessage = "Nội dung tin nhắn là bắt buộc.")]
    public string Content { get; set; } = string.Empty;

    public string MessageType { get; set; } = "Text"; // Text, ProductReference, System

    public List<long>? ProductIds { get; set; }

    public string? MetadataJson { get; set; }
}

public class GuestSessionInitRequestDto
{
    public Guid? GuestId { get; set; }
    public string? Token { get; set; }
    public long? BranchId { get; set; }
}

public class GuestMessageCreateDto
{
    [Required(ErrorMessage = "Chi nhánh là bắt buộc.")]
    public long BranchId { get; set; }

    public long? ConversationId { get; set; }

    public string? Content { get; set; }

    public string MessageType { get; set; } = "Text"; // Text, ProductReference

    public List<long>? ProductIds { get; set; }

    public string? Topic { get; set; }
    public string? Subject { get; set; }
}

public class ProductConsultationRequestDto
{
    [Required(ErrorMessage = "Chi nhánh là bắt buộc.")]
    public long BranchId { get; set; }

    public long? ConversationId { get; set; }

    [Required(ErrorMessage = "Danh sách món ăn là bắt buộc.")]
    public List<long> ProductIds { get; set; } = new List<long>();

    public string? Note { get; set; }
}

public class ChatRetentionSettingDto
{
    [Required(ErrorMessage = "Chi nhánh là bắt buộc.")]
    public long BranchId { get; set; }

    [Required(ErrorMessage = "Thời gian lưu trữ (phút) là bắt buộc.")]
    [Range(1, 525600, ErrorMessage = "Thời gian lưu trữ phải từ 1 phút đến 365 ngày.")]
    public int RetentionMinutes { get; set; } = 43200;
}
#endregion

#region Response DTOs
public class GuestSessionResponseDto
{
    public Guid GuestId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActiveTime { get; set; }
}

public class ProductReferenceItemDto
{
    [JsonPropertyName("productId")]
    public long ProductId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("imageLink")]
    public string ImageLink { get; set; } = string.Empty;

    [JsonPropertyName("groupName")]
    public string GroupName { get; set; } = string.Empty;
}

public class ConversationViewDto
{
    public long Id { get; set; }
    public long? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public long? GuestChatSessionId { get; set; }
    public string? GuestName { get; set; }
    public long BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string Topic { get; set; } = "General";
    public string? Subject { get; set; }
    public string? PreferredContactMethod { get; set; }
    public long? AssignedEmployeeId { get; set; }
    public string? AssignedEmployeeName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int UnreadCount { get; set; }
    public string? LastMessage { get; set; }
}

public class MessageViewDto
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public long? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public long? GuestChatSessionId { get; set; }
    public string? GuestName { get; set; }
    public long? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string MessageType { get; set; } = "Text";
    public string Content { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public string? Topic { get; set; }
    public string? Subject { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class ConversationAssignmentHistoryViewDto
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public long? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public string? CreatorName { get; set; }
}
#endregion
