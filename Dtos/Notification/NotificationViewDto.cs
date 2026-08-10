using System;

namespace MenuGoBE.Dtos.Notification
{
    public class NotificationViewDto
    {
        public long Id { get; set; }
        public long BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsImportant { get; set; }
        public string Priority { get; set; } = "Information";
        public string? ReferenceType { get; set; }
        public long? ReferenceId { get; set; }
        public string? RedirectUrl { get; set; }
    }
}
