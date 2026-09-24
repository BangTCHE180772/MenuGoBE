namespace MenuGoBE.Dtos.WorkSchedule
{
    public class CurrentShiftSessionDto
    {
        public bool Authorized { get; set; }
        public string DenyReason { get; set; } = string.Empty;

        // Thông tin ca làm hiện tại (null nếu không authorized)
        public string ShiftName { get; set; } = string.Empty;
        public string ShiftTime { get; set; } = string.Empty; // "HH:mm - HH:mm"
        public DateTime? ShiftEndTime { get; set; }
        public DateTime? DisconnectTime { get; set; }
        public long BranchId { get; set; }
    }
}
