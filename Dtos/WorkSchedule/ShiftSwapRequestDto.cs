using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.WorkSchedule
{
    public class ShiftSwapRequestDto
    {
        [Required]
        public long WorkScheduleId { get; set; }

        [Required]
        public long TargetAccountId { get; set; }

        public string Note { get; set; } = string.Empty;
    }
}
