using MenuGoBE.Dtos.Product;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Interface.Services
{
    public interface IProductService
    {
        Task<ProductViewDto?> GetByIdAsync(long id);
        Task<List<ProductViewDto>> GetAllAsync(ProductType? type = null);
        Task<List<ProductViewDto>> GetByTypeAsync(string type);
        Task<ProductViewDto?> GetByIdAndTypeAsync(long id, string type);
        Task<ProductViewDto?> GetByIdAndTypeAsync(long id, ProductType type);
        Task<ProductViewDto> CreateAsync(ProductCreateDto dto, ProductType? overrideType = null);
        Task<ProductViewDto> CreateWithTypeAsync(ProductCreateDto dto, string type);
        Task<ProductViewDto> CreateProcessedAsync(ProcessedProductCreateDto dto);
        Task<ProductViewDto> CreateManufacturedAsync(ManufacturedProductCreateDto dto);
        Task<ProductViewDto> CreateRegularAsync(RegularProductCreateDto dto);
        Task<ProductViewDto> CreateIngredientAsync(IngredientProductCreateDto dto);
        Task<ProductViewDto> CreateToolAsync(ToolProductCreateDto dto);

        Task<bool> UpdateAsync(ProductUpdateDto dto, ProductType? overrideType = null);
        Task<bool> UpdateWithTypeAsync(ProductUpdateDto dto, string type);
        Task<bool> UpdateProcessedAsync(ProcessedProductUpdateDto dto);
        Task<bool> UpdateManufacturedAsync(ManufacturedProductUpdateDto dto);
        Task<bool> UpdateRegularAsync(RegularProductUpdateDto dto);
        Task<bool> UpdateIngredientAsync(IngredientProductUpdateDto dto);
        Task<bool> UpdateToolAsync(ToolProductUpdateDto dto);
        Task<bool> DeleteAsync(long id, ProductType? expectedType = null);
        Task<bool> DeleteWithTypeAsync(long id, string type);

        /// <summary>
        /// Cập nhật giá bán hàng loạt. Chỉ hỗ trợ Regular, Manufactured, Processed.
        /// Trả về số lượng sản phẩm đã cập nhật thành công.
        /// </summary>
        Task<int> BulkUpdateSellPriceAsync(List<SellPriceBulkUpdateItemDto> items);
    }
}
