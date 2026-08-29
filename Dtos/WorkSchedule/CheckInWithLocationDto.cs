namespace MenuGoBE.Dtos.WorkSchedule
{
    public class CheckInWithLocationDto
    {
        /// <summary>ID chi nhánh nhân viên muốn điểm danh</summary>
        public long BranchId { get; set; }

        /// <summary>Vĩ độ (latitude) từ GPS của thiết bị</summary>
        public double Latitude { get; set; }

        /// <summary>Kinh độ (longitude) từ GPS của thiết bị</summary>
        public double Longitude { get; set; }

        /// <summary>ID ca làm việc cụ thể nếu nhân viên chọn ca để điểm danh (tùy chọn)</summary>
        public long? WorkScheduleId { get; set; }
    }
}
