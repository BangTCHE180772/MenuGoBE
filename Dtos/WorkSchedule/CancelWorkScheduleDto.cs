using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.WorkSchedule
{
    public class CancelWorkScheduleDto
    {
        [Required(ErrorMessage = "Phải nhập lý do hủy ca.")]
        public string Reason { get; set; } = string.Empty;
    }
}
