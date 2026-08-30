using System.Text.Json.Serialization;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.WorkSchedule
{
    public class ShiftChangeRequestViewDto
    {
        public long Id { get; set; }
        public long WorkScheduleId { get; set; }
        public long AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public long BranchId { get; set; }
        public DateOnly WorkDate { get; set; }

        public long OldShiftId { get; set; }
        public string OldShiftName { get; set; } = string.Empty;
        public TimeOnly OldStartTime { get; set; }
        public TimeOnly OldEndTime { get; set; }

        public long NewShiftId { get; set; }
        public string NewShiftName { get; set; } = string.Empty;
        public TimeOnly NewStartTime { get; set; }
        public TimeOnly NewEndTime { get; set; }
        public bool IsNightShift { get; set; }

        public string? Reason { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShiftChangeRequestStatus Status { get; set; }

        public DateTime RequestedAt { get; set; }

        public long? ApprovedBy { get; set; }
        public string? ApproverName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectReason { get; set; }
    }
}
