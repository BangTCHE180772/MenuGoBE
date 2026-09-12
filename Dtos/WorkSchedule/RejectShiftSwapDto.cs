using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.WorkSchedule
{
    public class RejectShiftSwapDto
    {
        [Required]
        public long SenderAccountId { get; set; }

        [Required(ErrorMessage = "Phải nhập lý do từ chối/hủy yêu cầu đổi ca.")]
        public string Note { get; set; } = string.Empty;
    }
}
