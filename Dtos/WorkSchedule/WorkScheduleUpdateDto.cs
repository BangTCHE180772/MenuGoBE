using System;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.WorkSchedule
{
    public class WorkScheduleUpdateDto
    {
        [Required]
        public long Id { get; set; }

        [Required]
        public long AccountId { get; set; }

        [Required]
        public long BranchId { get; set; }

        [Required]
        public long ShiftId { get; set; }

        public string Code { get; set; } = string.Empty;

        [Required]
        public DateOnly WorkDate { get; set; }

        public DateTime? CheckInAt { get; set; }
        public DateTime? CheckOutAt { get; set; }
        public decimal? ActualHours { get; set; }
        public decimal? ApprovedOTHours { get; set; }
        public string? OTStatus { get; set; }
        public string? OTNotes { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool ManagerApproved { get; set; }
        public bool IsHeadChef { get; set; }
    }
}
