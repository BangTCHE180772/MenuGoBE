namespace MenuGoBE.Dtos.Order
{
    public class BatchCookingResultDto
    {
        // Số dòng món đã chuyển sang trạng thái mới trong mẻ.
        public int Started { get; set; }

        // Lý do từng món không chuyển được (thiếu nguyên liệu, ...), kèm tên bàn.
        public List<string> Errors { get; set; } = new();
    }
}
