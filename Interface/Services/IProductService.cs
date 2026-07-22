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
        Task<bool> UpdateAsync(ProductUpdateDto dto, ProductType? overrideType = null);
        Task<bool> UpdateWithTypeAsync(ProductUpdateDto dto, string type);
        Task<bool> DeleteAsync(long id, ProductType? expectedType = null);
        Task<bool> DeleteWithTypeAsync(long id, string type);
    }
}
