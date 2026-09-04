namespace MenuGoBE.Dtos.Leftover
{
    // Món thừa (Return, Processed) còn trong cửa sổ 30 phút và chưa được dùng lại —
    // hiển thị làm gợi ý khi có đơn khác cần đúng món đó.
    public class LeftoverReusableDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? TableName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
