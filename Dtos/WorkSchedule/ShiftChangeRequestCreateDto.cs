namespace MenuGoBE.Dtos.WorkSchedule
{
    public class ShiftChangeRequestCreateDto
    {
        public long WorkScheduleId { get; set; }

        /// <summary>Chọn ca mẫu có sẵn (ưu tiên nếu có).</summary>
        public long? NewShiftId { get; set; }

        /// <summary>Hoặc nhập giờ ca mới tùy chỉnh (bắt buộc cả 2 nếu không chọn NewShiftId).</summary>
        public TimeOnly? NewStartTime { get; set; }
        public TimeOnly? NewEndTime { get; set; }

        public string? Reason { get; set; }
    }
}
