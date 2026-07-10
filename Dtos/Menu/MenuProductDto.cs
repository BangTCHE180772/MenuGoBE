namespace MenuGoBE.Dtos.Menu
{
    /// <summary>
    /// Dùng để thêm hoặc xóa 1 product khỏi menu.
    /// </summary>
    public class MenuProductDto
    {
        public long MenuId { get; set; }
        public long ProductId { get; set; }
    }
}
