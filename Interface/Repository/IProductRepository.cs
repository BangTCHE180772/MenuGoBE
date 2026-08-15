using MenuGoBE.Dtos.Product;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Interface.Repository
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(long id);
        Task<List<Product>> GetAllAsync(ProductType? type = null);
        Task<List<Product>> GetByTypeAsync(string type);
        Task<Product?> GetByIdAndTypeAsync(long id, string type);
        Task<Product?> GetByIdAndTypeAsync(long id, ProductType type);
        Task CreateAsync(Product entity);
        Task UpdateAsync(Product entity);
        Task DeleteAsync(Product entity);
        Task<bool> ExistsByNameAsync(string name);
        Task<bool> ExistsByNameExcludeIdAsync(string name, long id);
        Task<bool> ExistsBySKUAsync(string sku);
        Task<bool> ExistsBySKUExcludeIdAsync(string sku, long id);
        Task ClearUnitConversionsAsync(long productId);
        Task SyncUnitConversionsAsync(long productId, List<ProductUnitConversionDto> conversions);
        Task<ProductType?> GetProductTypeEnumAsync(long id);
        Task<string?> GetProductTypeAsync(long id);
        Task SaveChangesAsync();
    }
}
