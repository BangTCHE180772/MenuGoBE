namespace MenuGoBE.Dtos.Menu
{
    /// <summary>
    /// Dùng để thêm hoặc xóa nhiều product khỏi menu cùng lúc.
    /// </summary>
    public class MenuProductsBulkDto
    {
        public long MenuId { get; set; }
        public List<long> ProductIds { get; set; } = new();
    }
}
