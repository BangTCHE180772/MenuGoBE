using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.WorkSchedule;

public class WorkScheduleOTApproveDto
{
    public long WorkScheduleId { get; set; }

    [Range(0, 24, ErrorMessage = "Số giờ OT phải từ 0 đến 24h")]
    public decimal ApprovedOTHours { get; set; }

    public string OTStatus { get; set; } = "Approved"; // Approved | Rejected

    public string OTNotes { get; set; } = string.Empty;
}

public class WorkSchedulePOSActivityDto
{
    public long WorkScheduleId { get; set; }
    public long AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public DateTime? CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public decimal ActualHours { get; set; }
    public decimal StandardHours { get; set; }
    public decimal ExcessHours { get; set; } // ActualHours - StandardHours
    
    public int OrderCountDuringExcessTime { get; set; }
    public decimal TotalOrderAmountDuringExcessTime { get; set; }
    public bool HasPOSActivity { get; set; }
    public string SuspicionReason { get; set; } = string.Empty; // e.g. "Không phát sinh đơn POS sau giờ ca -> Nghi vấn quên check-out"
}
