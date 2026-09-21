using System;

namespace MenuGoBE.Dtos.Notification
{
    public class NotificationQueryDto
    {
        public long? BranchId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Type { get; set; }
        public string? Priority { get; set; }
        public bool? IsRead { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
