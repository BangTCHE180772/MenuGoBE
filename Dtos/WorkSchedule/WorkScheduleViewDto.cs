using System;

namespace MenuGoBE.Dtos.WorkSchedule
{
    public class WorkScheduleViewDto
    {
        public long Id { get; set; }
        public long AccountId { get; set; }
        public long BranchId { get; set; }
        public long ShiftId { get; set; }
        public string Code { get; set; } = string.Empty;
        public DateOnly WorkDate { get; set; }
        public DateTime? CheckInAt { get; set; }
        public DateTime? CheckOutAt { get; set; }
        public decimal? ActualHours { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool ManagerApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
