using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.WorkSchedule
{
    public class LeaveRequestDto
    {
        [Required]
        public long WorkScheduleId { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}
