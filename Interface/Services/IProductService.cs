using MenuGoBE.Dtos.Product;

namespace MenuGoBE.Interface.Services
{
    public interface IProductService
    {
        Task<List<ProductViewDto>> GetByTypeAsync(string type);
        Task<ProductViewDto?> GetByIdAndTypeAsync(long id, string type);
        Task<ProductViewDto> CreateWithTypeAsync(ProductCreateDto dto, string type);
        Task<bool> UpdateWithTypeAsync(ProductUpdateDto dto, string type);
        Task<bool> DeleteWithTypeAsync(long id, string type);
    }
}
