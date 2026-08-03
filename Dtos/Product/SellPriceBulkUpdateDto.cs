namespace MenuGoBE.Dtos.Product
{
    /// <summary>
    /// Dùng cho API PATCH /api/Product/sell-price
    /// Mỗi item chứa Id sản phẩm và giá bán mới.
    /// Chỉ hỗ trợ loại Regular, Manufactured, Processed.
    /// </summary>
    public class SellPriceBulkUpdateItemDto
    {
        public long Id { get; set; }
        public decimal SellPrice { get; set; }
    }
}
